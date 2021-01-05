 using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class CustomServer : GStoreServices.GStoreServicesBase
    {

        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private string server_id;
        private string url;
        private int min_delay;
        private int max_delay;

        //shows for each partition the server is, if he is the master for that partition
        private List<(string, bool)> responsability_map;

        private LinkedList<Dictionary<string, LinkedList<Server.PlaceholderServer>>> mapping = new LinkedList<Dictionary<string, LinkedList<PlaceholderServer>>>();
        
        //this attribute is here just to prevent the scripts from being executed before the reception of the partitions
        private bool is_partition_ready = false;
        
        //keep record of the objects this server has saved
        private LinkedList<DataStoreObject> saved_objects;

        //read requests to be served
        private LinkedList<ReadObjectClient> waiting_read = new LinkedList<ReadObjectClient>();

        //write requests to be served
        private LinkedList<WriteObjectClient> waiting_write = new LinkedList<WriteObjectClient>();

        //monitoring if the server is writing something (useful for inserting new incoming write requests 
        private bool is_processing_write = false;

        //boolean to indicate if the server is in Freez state or not
        private bool is_freez = false;

        //list of crashed servers (to display when status message comes)
        //(server_id, partition_id)
        LinkedList<(string, string)> crashed_servers = new LinkedList<(string, string)>();

        //this represents the objects that are locked during write operations
        private List<string> objects_locked_write = new List<string>();


        public CustomServer(string server_id, string url, int min_delay, int max_delay, List<(string, bool)> master_map)
        {
            this.server_id = server_id;
            this.url = url;
            this.min_delay = min_delay;
            this.max_delay = max_delay;
            this.responsability_map = master_map;
            saved_objects = new LinkedList<DataStoreObject>();
        }

        public void startServer()
        {
            Console.WriteLine($"Starting server number {this.server_id}...............");
            Grpc.Core.Server server2 = new Grpc.Core.Server
            {
                Services = { GStoreServices.BindService(this) },
                Ports = { new ServerPort("localhost", Int32.Parse(url.Split(":")[2]), ServerCredentials.Insecure) }
            };
            server2.Start();
            Console.WriteLine($"Server number {this.server_id} is now listening...............");
            Console.ReadKey();
            server2.ShutdownAsync().Wait();
        }

       
        ////////////////////////////**********************ONLY MASTER SERVER****************************///////////////////////////////

        //implements Write service
        public override Task<WriteObjectClientACK> Write(WriteObjectClient request, ServerCallContext context)
        {
            Console.WriteLine("****************************************************************************************");
            Console.WriteLine($"Server {this.server_id} is performing a write...");
            Console.WriteLine($"Objeto a escrever: {request.Objectid} {request.Object} na Partição {request.Partid}");
            Console.WriteLine("****************************************************************************************");

            //delay write
            Random rnd = new Random();
            Thread.Sleep(rnd.Next(this.min_delay,this.max_delay));

            //mesmo que o servidor esteja freezed, aqui é feito o armazenamento dos pedidos
            lock (waiting_write)
            {
                Console.WriteLine($"Server {this.server_id} is adding a write request to the list...");
                this.waiting_write.AddLast(request);
            }
            return Write_aux(request);
        }
    
        //implements the Write function of the server (writes a specific object)
        private async Task<WriteObjectClientACK> Write_aux(WriteObjectClient request)
        {
            //ponto crítico para o freeze (onde é feita a execução do processo write)
            while (this.is_freez) {}

            //second suggestion for semaphore position
            await semaphoreSlim.WaitAsync();

            bool success = false;
            Console.WriteLine("Vou começar a executar a função Write_aux");
            //a escrita só pode ser feita por um servidor que seja master (criar uma classe para quem seja master)
            if (checkMasterForPartition(request.Partid))
            {
                Console.WriteLine("Vou fazer a escrita num Master Server");
                if (this.is_processing_write)
                {
                    Console.WriteLine("************************************************************************************");
                    Console.WriteLine("Servidor a processar pedidos. Pedido vai ser executado mais tarde");
                    Console.WriteLine("************************************************************************************");
                }
                else
                {
                    //servidor vai processar pedido write
                    this.is_processing_write = true;

                    Console.WriteLine("************************************************************************************");
                    Console.WriteLine("Servidor vai processar um novo pedido. Vamos retirar este pedido da lista");
                    Console.WriteLine("************************************************************************************");

                    Console.WriteLine($"Comprimento da lista antes do processamento: {waiting_write.Count}");

                    //remover o pedido a ser executado da lista
                    WriteObjectClient list_request = waiting_write.ElementAt(0);
                    waiting_write.RemoveFirst();

                    //executar pedido 

                    //verificar se o objeto existe guardado na lista de objetos de servidor
                    if (!exists_in_server(request.Objectid,request.Partid)) {
                        Console.WriteLine("Server does not have the object...");
                        //objeto não existe como estando guardado
                        //vamos guardar o seu valor e propagar para as réplicas
                        DataStoreObject obj_aux = new DataStoreObject(request.Partid, request.Objectid, request.Object);
                        lock (this)
                        {
                            this.saved_objects.AddLast(obj_aux);
                        }
                                                
                        //propagar objeto pelas réplicas
                        foreach (var partition in mapping)
                        {
                            if (partition.ContainsKey(obj_aux.Partition_id))
                            {
                                RepeatReplication:
                                Console.WriteLine("Propagating object to servers in the partition");
                                //isto é a nossa partição
                                LinkedList<Server.PlaceholderServer> partition_servers = partition[obj_aux.Partition_id];

                                //para cada servidor da partição
                                //replicar objeto
                                int number_of_acks_after_replication = await ReliableBroadcastReplicatekWithACKS(partition_servers, new CreateObjectInReplicas
                                {
                                    Partid = list_request.Partid,
                                    Objectid = list_request.Objectid,
                                    Object = list_request.Object
                                });
                                Console.WriteLine($"Isto é o número de ACKS da replication: {number_of_acks_after_replication}");

                                //comparar numero de acks da replication com o número de réplicas existentes
                                if (number_of_acks_after_replication == (partition_servers.Count - 1))
                                {
                                    Console.WriteLine("Todas as réplicas fizeram já a replicação.");
                                    success = true;
                                }
                                else
                                {
                                    Console.WriteLine("*****************************************");
                                    Console.WriteLine("ALGUMA DAS RÉPLICAS CRASHOU....");
                                    Console.WriteLine("*****************************************");

                                    //Alguma das réplicas crashou

                                    //Descobrir quem crashou
                                    LinkedList<string> non_crashed_servers = await CheckWhoIsAlive(partition_servers);

                                    //iterate over current partition
                                    foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition2 in mapping)
                                    {
                                        //LinkedList<PlaceholderServer> crashed_nodes = new LinkedList<PlaceholderServer>();
                                        foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition2)
                                        {
                                            if (entry.Key == obj_aux.Partition_id)
                                            {
                                                LinkedList<PlaceholderServer> crashed_nodes = new LinkedList<PlaceholderServer>();
                                                partition_servers = entry.Value;
                                                //partição deste master
                                                foreach (PlaceholderServer place_server in entry.Value)
                                                {
                                                    if (!non_crashed_servers.Contains(place_server.Server_id))
                                                    {
                                                        //servidor crashou
                                                        crashed_nodes.AddLast(place_server);
                                                    }
                                                }

                                                //propagate changes to puppetmaster
                                                CrashedReplicaPM crashed_replica = new CrashedReplicaPM();
                                                crashed_replica.Partitionid = entry.Key;

                                                //delete crashed servers from the master partition
                                                foreach (PlaceholderServer ps in crashed_nodes)
                                                {
                                                    entry.Value.Remove(ps);
                                                    crashed_replica.Serverid.Add(ps.Server_id);
                                                }

                                                //connect to PM and propagate
                                                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                                GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:10000");
                                                var pm = new PMServices.PMServicesClient(channel);
                                                pm.RemoveCrashedReplicaPM(crashed_replica);
                                            }
                                        }
                                    }

                                    //voltar a fazer o pedido de escrita para as réplicas
                                    goto RepeatReplication;
                                }

                                this.is_processing_write = false;
                                Console.WriteLine("Vamos libertar o cadeado........");
                                semaphoreSlim.Release();

                                return new WriteObjectClientACK
                                {
                                    Ok = true
                                };

                            }
                        }

                    }
                    else
                    {
                        //verificar a que objeto se estão a referir
                        DataStoreObject lock_obj = null;
                        foreach (DataStoreObject obj in saved_objects)
                        {
                            if (obj.Object_id == list_request.Objectid && obj.Partition_id==list_request.Partid)
                            {
                                Console.WriteLine("************************************************************************************");
                                Console.WriteLine($"LOCK_OBJ: SERVER HAS FOUND THE OBJECT. THE VALUE STORED AT MASTER CURRENTLY IS: {obj.Value}");
                                Console.WriteLine("************************************************************************************");
                                lock_obj = obj;
                                //dar lock ao objeto no lado do servidor para que não possam existir leitura sobre este objeto
                                lock_obj.Is_locked_object = true;
                            }
                        }

                        RepeatLock:

                        LinkedList<Server.PlaceholderServer> partition_servers = null;

                        foreach (var partition in mapping)
                        {
                            if (partition.ContainsKey(lock_obj.Partition_id))
                            {
                                //isto é a nossa partição
                                partition_servers = partition[lock_obj.Partition_id];
                            }
                        }

                        try
                        {
                            // The critical section.
                            //implementar reliable broadcast (ver número de réplicas existentes no mapeamento)
                            int number_of_acks_lock_request = await ReliableBroadcastLockWithACKS(partition_servers, new LockObjectInfo
                            {
                                Partid = lock_obj.Partition_id,
                                Objectid = lock_obj.Object_id
                            });

                            Console.WriteLine($"Isto é o número de ACKs do lock: {number_of_acks_lock_request}");

                            //depois de receber o ACK, vai então mudar
                            if (number_of_acks_lock_request == (partition_servers.Count - 1))
                            {
                                Console.WriteLine("Master server vai mudar o valor do objecto.....");
                                Console.WriteLine("************************************************************************************");
                                Console.WriteLine($"SERVER HAS FOUND THE OBJECT. THE VALUE STORED AT MASTER CURRENTLY IS: {lock_obj.Value}");
                                Console.WriteLine("************************************************************************************");
                                lock_obj.Value = list_request.Object;
                                Console.WriteLine($"O novo valor do objeto no MASTER SERVER é: {lock_obj.Value}");
                            }
                            else
                            {
                                Console.WriteLine("*****************************************");
                                Console.WriteLine("ALGUMA DAS RÉPLICAS CRASHOU....");
                                Console.WriteLine("*****************************************");

                                //Alguma das réplicas crashou

                                //Descobrir quem crashou
                                LinkedList<string> non_crashed_servers = await CheckWhoIsAlive(partition_servers);

                                //iterate over current partition
                                foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition2 in mapping)
                                {
                                    //LinkedList<PlaceholderServer> crashed_nodes = new LinkedList<PlaceholderServer>();
                                    foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition2)
                                    {
                                        if (entry.Key == lock_obj.Partition_id)
                                        {
                                            LinkedList<PlaceholderServer> crashed_nodes = new LinkedList<PlaceholderServer>();
                                            partition_servers = entry.Value;
                                            //partição deste master
                                            foreach (PlaceholderServer place_server in entry.Value)
                                            {
                                                if (!non_crashed_servers.Contains(place_server.Server_id))
                                                {
                                                    //servidor crashou
                                                    crashed_nodes.AddLast(place_server);
                                                }
                                            }

                                            //propagate changes to puppetmaster
                                            CrashedReplicaPM crashed_replica = new CrashedReplicaPM();
                                            crashed_replica.Partitionid = entry.Key;

                                            //delete crashed servers from the master partition
                                            foreach (PlaceholderServer ps in crashed_nodes)
                                            {
                                                entry.Value.Remove(ps);
                                                crashed_replica.Serverid.Add(ps.Server_id);
                                            }

                                            //connect to PM and propagate
                                            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:10000");
                                            var pm = new PMServices.PMServicesClient(channel);
                                            pm.RemoveCrashedReplicaPM(crashed_replica);
                                        }
                                    }
                                }

                                //voltar a fazer o pedido de escrita para as réplicas
                                goto RepeatLock;

                            }

                            RepeatUpdate:
                            //enviar para todas as réplicas a alteração
                            int number_of_acks_after_update = await ReliableBroadcastUpdatekWithACKS(partition_servers, new UpdateObjectInfo
                            {
                                Partid = lock_obj.Partition_id,
                                Objectid = lock_obj.Object_id,
                                Objectvalue = list_request.Object
                            });
                            Console.WriteLine($"Isto é o número de ACKS do update: {number_of_acks_after_update}");


                            //verificar se recebemos todos os ACK's
                            if (number_of_acks_after_update == (partition_servers.Count - 1))
                            {
                                Console.WriteLine("Todas as réplicas fizeram já a alteração.");
                                success = true;
                            }
                            else
                            {
                                Console.WriteLine("*****************************************");
                                Console.WriteLine("ALGUMA DAS RÉPLICAS CRASHOU....");
                                Console.WriteLine("*****************************************");

                                //Alguma das réplicas crashou

                                //Descobrir quem crashou
                                LinkedList<string> non_crashed_servers = await CheckWhoIsAlive(partition_servers);

                                //iterate over current partition
                                foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition2 in mapping)
                                {
                                    //LinkedList<PlaceholderServer> crashed_nodes = new LinkedList<PlaceholderServer>();
                                    foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition2)
                                    {
                                        if (entry.Key == lock_obj.Partition_id)
                                        {
                                            LinkedList<PlaceholderServer> crashed_nodes = new LinkedList<PlaceholderServer>();
                                            partition_servers = entry.Value;
                                            //partição deste master
                                            foreach (PlaceholderServer place_server in entry.Value)
                                            {
                                                if (!non_crashed_servers.Contains(place_server.Server_id))
                                                {
                                                    //servidor crashou
                                                    crashed_nodes.AddLast(place_server);
                                                }
                                            }

                                            //propagate changes to puppetmaster
                                            CrashedReplicaPM crashed_replica = new CrashedReplicaPM();
                                            crashed_replica.Partitionid = entry.Key;

                                            //delete crashed servers from the master partition
                                            foreach (PlaceholderServer ps in crashed_nodes)
                                            {
                                                entry.Value.Remove(ps);
                                                crashed_replica.Serverid.Add(ps.Server_id);
                                            }

                                            //connect to PM and propagate
                                            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:10000");
                                            var pm = new PMServices.PMServicesClient(channel);
                                            pm.RemoveCrashedReplicaPM(crashed_replica);
                                        }
                                    }
                                }

                                //voltar a fazer o pedido de escrita para as réplicas
                                goto RepeatUpdate;

                            }

                        }
                        finally
                        {
                            //When the task is ready, release the semaphore. It is vital to ALWAYS release the semaphore when we are ready, or else we will end up with a Semaphore that is forever locked.
                            //This is why it is important to do the Release within a try...finally clause; program execution may crash or take a different path, this way you are guaranteed execution
                            Console.WriteLine("Vamos libertar o cadeado........");
                            semaphoreSlim.Release();
                        }

                        if (success)
                        {
                            this.is_processing_write = false;
                            Console.WriteLine("Vamos retornar ao cliente...");

                            Console.WriteLine($"Comprimento da lista após processamento: {waiting_write.Count}");

                            if (waiting_write.Count != 0)
                            {
                                var thread = new Thread(() => Write_aux(waiting_write.ElementAt(0)));
                                thread.Start();
                            }

                            return new WriteObjectClientACK
                            {
                                Ok = true
                            };
                        }
                    }
                
                }
            }

            return new WriteObjectClientACK
            {
                Ok = false
            };
        }

        private bool checkMasterForPartition(string partition_id)
        {
            Console.WriteLine($"Checking if {this.server_id} is master for {partition_id}");
            bool ismaster = false;
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    if(entry.Key == partition_id) {
                        foreach (PlaceholderServer place_server in entry.Value)
                        {
                            if (place_server.Server_id==this.server_id && place_server.Is_master)
                            {
                                ismaster = true;
                                goto Finish;
                            }
                        }
                    }
                }
            }

            Finish:
                Console.WriteLine($"Is {this.server_id} master for {partition_id}? : {ismaster}");
                return ismaster;
        }

        private async Task<LinkedList<string>> CheckWhoIsAlive(LinkedList<Server.PlaceholderServer> partition_servers)
        {
            List<Task<IsAliveMessageACK>> tasks = new List<Task<IsAliveMessageACK>>();

            //por cada servidor da nossa partição, vamos enviar a mensagem genérica Ts (pode ser LockObject ou UpdateObject)
            //temos que excluir o nosso próprio servidor
            foreach (PlaceholderServer c in partition_servers)
            {
                if (c.Server_id != this.server_id)
                {
                    //ligar ao servidor correspondente
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    GrpcChannel channel = GrpcChannel.ForAddress(c.Url);
                    var client = new GStoreServices.GStoreServicesClient(channel);
                    tasks.Add(Task.Run(() => client.IsAliveServer(new IsAliveMessage { })));
                }
            }

            //espera pela execução da ação mencionada anteriormente
            var results = await Task.WhenAll(tasks);

            LinkedList<string> result_list = new LinkedList<string>();

            foreach(IsAliveMessageACK message_ack in results)
            {
                result_list.AddLast(message_ack.Serverid);
            }

            return result_list;
        }

        public override Task<IsAliveMessageACK> IsAliveServer(IsAliveMessage request, ServerCallContext context)
        {
            return Task.FromResult(IsAliveServer_aux(request));
        }

        private IsAliveMessageACK IsAliveServer_aux(IsAliveMessage request)
        {
            return new IsAliveMessageACK
            {
                Serverid = this.server_id
            };
        }

        private async Task<int> ReliableBroadcastLockWithACKS(LinkedList<Server.PlaceholderServer> partition_servers, LockObjectInfo message)
        {
            List<Task<LockObjectACK>> tasks = new List<Task<LockObjectACK>>();

            //por cada servidor da nossa partição, vamos enviar a mensagem genérica Ts (pode ser LockObject ou UpdateObject)
            //temos que excluir o nosso próprio servidor
            foreach (PlaceholderServer c in partition_servers)
            {
                if (c.Server_id != this.server_id)
                {
                    //ligar ao servidor correspondente
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    GrpcChannel channel = GrpcChannel.ForAddress(c.Url);
                    var client = new GStoreServices.GStoreServicesClient(channel);
                    tasks.Add(Task.Run(() => client.LockObject(message)));
                }
            }

            //espera pela execução da ação mencionada anteriormente
            var results = await Task.WhenAll(tasks);

            return results.Length;
        }

        private async Task<int> ReliableBroadcastUpdatekWithACKS(LinkedList<Server.PlaceholderServer> partition_servers, UpdateObjectInfo message)
        {
            List<Task<UpdateObjectACK>> tasks = new List<Task<UpdateObjectACK>>();

            //por cada servidor da nossa partição, vamos enviar a mensagem genérica Ts (pode ser LockObject ou UpdateObject)
            //temos que excluir o nosso próprio servidor
            foreach (PlaceholderServer c in partition_servers)
            {
                if (c.Server_id != this.server_id)
                {
                    //ligar ao servidor correspondente
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    GrpcChannel channel = GrpcChannel.ForAddress(c.Url);
                    var client = new GStoreServices.GStoreServicesClient(channel);
                    tasks.Add(Task.Run(() => client.UpdateObject(message) ));
                }
            }

            //espera pela execução da ação mencionada anteriormente
            var results = await Task.WhenAll(tasks);

            return results.Length;
        }

        private async Task<int> ReliableBroadcastReplicatekWithACKS(LinkedList<Server.PlaceholderServer> partition_servers, CreateObjectInReplicas message)
        {
            List<Task<CreateObjectInReplicasACK>> tasks = new List<Task<CreateObjectInReplicasACK>>();

            //por cada servidor da nossa partição, vamos enviar a mensagem para replicar o objeto
            //temos que excluir o nosso próprio servidor
            foreach (PlaceholderServer c in partition_servers)
            {
                if (c.Server_id != this.server_id)
                {
                    //ligar ao servidor correspondente
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    GrpcChannel channel = GrpcChannel.ForAddress(c.Url);
                    var client = new GStoreServices.GStoreServicesClient(channel);
                    tasks.Add(Task.Run(() => client.ReplicateObject(message)));
                }
            }

            //espera pela execução da ação mencionada anteriormente
            var results = await Task.WhenAll(tasks);

            return results.Length;
        }

        ////////////////////////////**************************************************///////////////////////////////


        //replicate object in replicas
        public override Task<CreateObjectInReplicasACK> ReplicateObject(CreateObjectInReplicas request, ServerCallContext context)
        {
            return ReplicateObject_aux(request);
        }

        private async Task<CreateObjectInReplicasACK> ReplicateObject_aux(CreateObjectInReplicas request)
        {
            Console.WriteLine("************************************************************************************");
            Console.WriteLine($"SERVER REPLICA WITH ID {this.server_id} HAS RECEIVED A NEW OBJECT. THE VALUE IS: {request.Object}");
            Console.WriteLine("************************************************************************************");
            lock (this)
            {
                this.saved_objects.AddLast(new DataStoreObject(request.Partid, request.Objectid, request.Object));
            }
            return new CreateObjectInReplicasACK
            {
                Ok = true
            };
        }

        public override Task<LockObjectACK> LockObject(LockObjectInfo request, ServerCallContext context)
        {
            return LockObject_aux(request);
        }

        private async Task<LockObjectACK> LockObject_aux(LockObjectInfo request)
        {
            Console.WriteLine("This is a lock object message...");
            foreach (DataStoreObject obj in this.saved_objects)
            {
                if (obj.Object_id == request.Objectid && obj.Partition_id==request.Partid)
                {
                    Console.WriteLine("Will get the object's lock....");
                    obj.Is_locked_object = true;
                    this.objects_locked_write.Add(obj.Object_id);
                    Console.WriteLine($"Could server {this.server_id} acquire the lock? : {obj.Is_locked_object}");
                }
            }
            Console.WriteLine("Vou sair do método lock.......");

            return new LockObjectACK()
            {
                Ok = true
            };
        }

        public override Task<UpdateObjectACK> UpdateObject(UpdateObjectInfo request, ServerCallContext context)
        {
            return Task.FromResult(UpdateObject_aux(request));
        }

        private UpdateObjectACK UpdateObject_aux(UpdateObjectInfo request)
        {
            Console.WriteLine("This is a update object message");
            Console.WriteLine($"O valor a atualizar nas réplicas é o seguinte: {request.Objectvalue}");
            foreach (DataStoreObject obj in this.saved_objects)
            {
                if (obj.Object_id == request.Objectid && obj.Object_id==request.Objectid)
                {
                    if (obj.Is_locked_object)
                    {
                        Console.WriteLine("Changing value....");
                        //update ao valor do objeto
                        obj.Value = request.Objectvalue;

                        Console.WriteLine("Vamos libertar o cadeado....");
                        obj.Is_locked_object = false;
                    }                    
                }
            }
            Console.WriteLine("Vou retornar no update ack.....");
            return new UpdateObjectACK()
            {
                Ok = true
            };
        }

        ////////////////////////////**********************READ****************************///////////////////////////////
        
        //implementing object read service
        public override Task<ObjectClient> Read(ReadObjectClient request, ServerCallContext context)
        {
            //delay read
            Random rnd = new Random();
            Thread.Sleep(rnd.Next(this.min_delay, this.max_delay));

            //onde o processo de leitura realmente começa (ponto crítico)
            while (this.is_freez) {}

            Console.WriteLine("************************************************************************************");
            Console.WriteLine("Leitura....");
            Console.WriteLine("************************************************************************************");
            return Read_auxAsync(request);
        }

        //implementing object read
        public async Task<ObjectClient> Read_auxAsync(ReadObjectClient request)
         {
            Console.WriteLine("************************************************************************************");
            Console.WriteLine($"SERVIDOR {this.server_id} VAI EFETUAR UMA LEITURA.");
            Console.WriteLine($"OBJETO {request.Objectid} VAI SER LIDO.");
            Console.WriteLine("************************************************************************************");
            string partition_id = request.Partid;
            string object_id = request.Objectid;

            //objeto existe na partição

            //verificar se o servidor atual tem o objeto
            if (this.exists_in_server(object_id,partition_id))
            {
                //servidor atual tem o objeto (então vai tentar retornar)
                foreach (var obj in saved_objects)
                {
                    if (obj.Object_id == object_id && obj.Partition_id == partition_id)
                    {
                        //check if object is not locked
                        if (!obj.Is_locked_object)
                        {
                            Console.WriteLine("************************************************************************************");
                            Console.WriteLine($"OBJETO {request.Objectid} ESTÁ LIVRE. VAI SER RETORNADO O SEGUINTE VALOR: {obj.Value}.");
                            Console.WriteLine("************************************************************************************");
                            //if it is unlocked return the object
                            return new ObjectClient
                            {
                                Objectclient = obj.Value
                            };
                        }
                        else
                        {
                            //else put the object in a FIFO and served only when it's unlocked
                            Console.WriteLine("************************************************************************************");
                            Console.WriteLine($"OBJETO {request.Objectid} ESTÁ OCUPADO. PEDIDO VAI SER COLOCADO NA FILA.");
                            Console.WriteLine("************************************************************************************");
                            waiting_read.AddLast(request);

                            //wait for execution in queue
                            string obj_value = await ReadValueFromQueue(obj);

                            Console.WriteLine("************************************************************************************");
                            Console.WriteLine($"VALOR RETORNADO DEPOIS DA ESPERA: {obj_value}.");
                            Console.WriteLine("************************************************************************************");

                            //after being executed from queue, return
                            return new ObjectClient
                            {
                                Objectclient = obj_value
                            };
                        }
                    }
                }
            }
            else
            {
                //servidor atual não tem o objeto, então temos que estabelecer ligação ao servidor que tem o objeto (vem no args do script)

                //fazer pedido para o GRPC para o cliente a indicar que vai acontecer detach

                //retorna para o cliente uma mensagem qualquer
                return new ObjectClient
                {
                    Objectclient = "some info..."
                };
            }

            return new ObjectClient
            {
                Objectclient = "some info..."
            };
         }

        public async Task<string> ReadValueFromQueue(DataStoreObject obj)
        {
            Console.WriteLine("GOING TO QUEUE TO EXECUTE MY PENDING REQUEST....");
            while (obj.Is_locked_object) { }
            Console.WriteLine($"GOT OUT FROM WHILE LOOP. GOING TO RETURN OBJECT VALUE. THIS IS THE VALUE {obj.Value}.");
            return obj.Value;
        }

        ////////////////////////////**************************************************///////////////////////////////

        //verifica se o objeto existe no servidor atual
        public bool exists_in_server(string object_id, string part_id)
        {
            if (this.saved_objects.Count != 0)
            {
                foreach (DataStoreObject obj in this.saved_objects)
                {
                    if(obj.Object_id == object_id && obj.Partition_id==part_id)
                    {
                        //objeto existe no servidor atual
                        return true;
                    }
                }
            }
            return false;
        }

        public override Task<ServerObject> RequestAllObjects(RequestAllObj request, ServerCallContext context)
        {
            //delay request all objects
            Random rnd = new Random();
            Thread.Sleep(rnd.Next(this.min_delay, this.max_delay));
            return Task.FromResult(RequestAllObjects_aux(request));
        }

        public ServerObject RequestAllObjects_aux(RequestAllObj request)
        {
            ServerObject server_object = new ServerObject();
            lock (this)
            {
                //iterate over every partition the server belongs
                foreach ((string part, bool ismaster) in this.responsability_map) {
                    if (part == request.PartId)
                    {
                        AllPartInfo a_partition = new AllPartInfo();
                        a_partition.PartId = part;
                        a_partition.Ismaster = ismaster;
                        foreach (DataStoreObject obj in this.saved_objects)
                        {
                            ObjectInfo my_obj = new ObjectInfo();
                            my_obj.Partitionid = part;
                            my_obj.Objectid = obj.Object_id;
                            my_obj.Objectvalue = obj.Value;
                            a_partition.Objects.Add(my_obj);
                        }
                        server_object.Partitions.Add(a_partition);
                    }
                }
            }
            return server_object;
        }

        public override Task<HasObjectACK> HasObject(HasObjectMsg request, ServerCallContext context)
        {
            //delay has objects
            Random rnd = new Random();
            Thread.Sleep(rnd.Next(this.min_delay, this.max_delay));

            while (this.is_freez) { }

            Console.WriteLine("************************************************************************************");
            Console.WriteLine("Checking if the server has the object on: HasOject");
            Console.WriteLine("************************************************************************************");

            return Task.FromResult(HasObject_aux(request));
        }

        public HasObjectACK HasObject_aux(HasObjectMsg request)
        {
            bool found_object = false;

            //iterate over saved objects
            foreach(DataStoreObject obj in this.saved_objects)
            {
                if(obj.Partition_id==request.Partid && obj.Object_id == request.Objectid)
                {
                    found_object = true;
                    goto TheEnd;
                }
            }

            TheEnd:
                return new HasObjectACK
                {
                    Ok = found_object
                };
        }

        /***********************************************************************************************************************/
        //Implementing receiving partitions from Puppet Master

        public override Task<PartitionACK> ReceivePartitionInfo(CompletePartition request, ServerCallContext context)
        {
            //delay delivery of partition
            Random rnd = new Random();
            Thread.Sleep(rnd.Next(this.min_delay, this.max_delay));
            Console.WriteLine($"Server {this.server_id} is receiving the partition from Puppet master.");
            return Task.FromResult(ReceivePartitionInfo_aux(request));
        }

        public PartitionACK ReceivePartitionInfo_aux(CompletePartition request)
        {
            foreach (PartitionInfo partition in request.Partitions)
            {
                Dictionary<string, LinkedList<Server.PlaceholderServer>> part = new Dictionary<string, LinkedList<PlaceholderServer>>();
                string partition_id = partition.Partitionid;
                LinkedList<Server.PlaceholderServer> part_servers = new LinkedList<PlaceholderServer>();
                foreach (ServerInfo server in partition.Servers)
                {
                    part_servers.AddLast(new PlaceholderServer(server.Serverid, server.Url, server.Ismaster));
                }

                part.Add(partition_id, part_servers);
                this.mapping.AddLast(part);
            }
            this.is_partition_ready = true;

            return new PartitionACK { };
        }


        /***********************************************************************************************************************/

        /***********************************************************************************************************************/
        //Implementing receiving status from puppet master
            public override Task<StatusServerAck> StatusServer(AskStatusServer request, ServerCallContext context)
            {
                //delay status
                Random rnd = new Random();
                Thread.Sleep(rnd.Next(this.min_delay, this.max_delay));
                return Task.FromResult(StatusClient_aux(request));
            }

            private StatusServerAck StatusClient_aux(AskStatusServer request)
            {
                Console.WriteLine("*************************************************");
                Console.WriteLine($"Nova mensagem de status recebida no servidor: {this.server_id}");
                Console.WriteLine($"Estado atual dos servidores no mapeamento:");
                foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
                {
                    foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                    {
                        Console.WriteLine($"#Partition {entry.Key}:");
                        foreach (PlaceholderServer place_server in entry.Value)
                        {
                            Console.WriteLine($"\t -> Server {place_server.Server_id}, Up: {place_server.Status}, Partition Master: {place_server.Is_master}");
                        }
                        //checking if there is crashed servers in that partition
                        foreach ((string serv_id, string part_id) in this.crashed_servers)
                        {
                            if (part_id == entry.Key)
                            {
                                Console.WriteLine($"\t -> Server {serv_id}, Up: Crashed");
                            }
                        }
                    }
                }
                Console.WriteLine("*************************************************");
                return new StatusServerAck { };
            }

        /***********************************************************************************************************************/

        /***********************************************************************************************************************/
        //Implementing freeze and unfreeze
        public override Task<FreezeMessageACK> FreezeServer(FreezeMessage request, ServerCallContext context)
        {
            //delay freeze
            Random rnd = new Random();
            Thread.Sleep(rnd.Next(this.min_delay, this.max_delay));

            return Task.FromResult(FreezeServer_aux(request));
        }

        public FreezeMessageACK FreezeServer_aux(FreezeMessage request)
        {
            this.is_freez = true;
            Console.WriteLine("***********************************************************");
            Console.WriteLine($"We did Freeze on Server {this.server_id}");
            Console.WriteLine("***********************************************************");

            return new FreezeMessageACK { };
        }

        public override Task<UnfreezeACK> UnfreezeServer(UnfreezeMessage request, ServerCallContext context)
        {
            //delay unfreeze
            Random rnd = new Random();
            Thread.Sleep(rnd.Next(this.min_delay, this.max_delay));

            return Task.FromResult(UnfreezeServer_aux(request));
        }

        public UnfreezeACK UnfreezeServer_aux(UnfreezeMessage request)
        {
            //change the boolean flag to false
            this.is_freez = false;
            Console.WriteLine("***********************************************************");
            Console.WriteLine($"We did Unfreeze on Server {this.server_id}");
            Console.WriteLine("***********************************************************");
            return new UnfreezeACK { };
        }

        /***********************************************************************************************************************/

        /***********************************************************************************************************************/
        //implementing updates on the partition

        public override Task<ServerCrashedACK> ServerCrasedServer(ServerCrashedID request, ServerCallContext context)
        {
            Console.WriteLine("*************************************************");
            Console.WriteLine($"Some server is down. Server {this.server_id} is updating partition");
            Console.WriteLine("*************************************************");
            return Task.FromResult(ServerCrashed_aux(request));
        }

        public ServerCrashedACK ServerCrashed_aux(ServerCrashedID request)
        {
            PlaceholderServer aux_server = null;
            string partition_id = "";
            int list_position = -1;
            List<(int, int)> to_delete = new List<(int, int)>();
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                list_position = list_position + 1;
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    int part_position = -1;
                    foreach (PlaceholderServer serv in entry.Value)
                    {
                        part_position = part_position + 1; 
                        if (serv.Server_id == request.Serverid)
                        {
                            //found the server to delete
                            aux_server = serv;
                            partition_id = entry.Key;

                            //add to be deleted later
                            to_delete.Add( (list_position,part_position) );

                            //add crashed server to crasher server list
                            this.crashed_servers.AddLast((aux_server.Server_id, partition_id));
                        }
                    }
                }
            }

                
            Console.WriteLine("*************************************************");
            Console.WriteLine($"Server {this.server_id} : Just updated partition. The server deleted was: {aux_server.Server_id}");
            Console.WriteLine("*************************************************");


            //iterate over the parts of the list to update
            foreach((int part, int serv) in to_delete)
            {
                Dictionary<string, LinkedList<Server.PlaceholderServer>>.ValueCollection a = this.mapping.ElementAt(part).Values;
                PlaceholderServer aux = a.ElementAt(0).ElementAt(serv);
                a.ElementAt(0).Remove(aux);
            }

            //server needs to check if the server crashed is a partition master
            //if master then run leader election
            if (aux_server.Is_master)
            {
                Console.WriteLine("----------------------------------------------------------------------------------------");
                Console.WriteLine("Vamos eleger um novo líder...");
                Console.WriteLine("----------------------------------------------------------------------------------------");
                LeaderElection(partition_id);

                //unlock every object locked, in the failed master partition, for write operations
                UnlockObjects(partition_id);
            }

                return new ServerCrashedACK { };
        }

        private void LeaderElection(string part_id)
        {
            List<string> servers_id = getListServerID(part_id);
            string remove_server = "";
            UpdateList:
            if (remove_server!="")
            {
                //significa que temos um leader que não é legível
                //então remover da lista de possíveis leaders
                servers_id.Remove(remove_server);
            }
            //selecionar novo possível leader
            string new_leader = getMaxServerID(servers_id);
            Console.WriteLine($"Primeira tentativa de líder: {new_leader}");
            //check if the chosen leader is master in another partition
            if (isAlreadyMaster(new_leader))
            {
            //temos que escolher novamente outra pessoa
                remove_server = new_leader;
                goto UpdateList;
            }
            else
            {
                //leader tem condições para ser eleito
                //percorrer a lista de mapeamento e na partição em questão (em que o master deu crash), temos que atualizar*
                //* o leader escolhido como sendo o novo master para a partição
                int list_position = -1;
                foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition2 in mapping)
                {
                    list_position = list_position + 1;
                    foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition2)
                    {
                        if (entry.Key==part_id)
                        {
                            LinkedList<Server.PlaceholderServer> part_servers = entry.Value;
                            foreach(PlaceholderServer place_server in part_servers)
                            {
                                if (place_server.Server_id == new_leader)
                                {
                                    place_server.Is_master = true;
                                    Console.WriteLine("********************************************************************");
                                    Console.WriteLine($"The new master (leader) as been nominated.");
                                    Console.WriteLine($"The new master is: {new_leader}");
                                    Console.WriteLine("********************************************************************");
                                    
                                    //se o servidor atual for o leader
                                    //temos que atualizar o responsability map
                                    if (this.server_id==new_leader)
                                    {
                                        Console.WriteLine("Vou atualizar o responsability map");
                                        int pos = -1;
                                        foreach ((string part, bool is_mast) in this.responsability_map)
                                        {
                                            pos = pos + 1;
                                            if (part == part_id)
                                            {
                                                goto Update;
                                            }
                                        }
                                        Update:
                                        this.responsability_map.RemoveAt(pos);
                                        this.responsability_map.Add( (part_id,true) );

                                        //send update to PM
                                        Console.WriteLine("Servidor leader vai enviar para puppetmaster que é leader...");
                                        //do client to server and propagate
                                        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                        GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:10000");
                                        var pm = new PMServices.PMServicesClient(channel);
                                        pm.UpdateLeader(new NewLeader
                                        {
                                            Leaderid = this.server_id,
                                            Partid = part_id
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool isAlreadyMaster(string server_id)
        {
            bool is_master = false;
            //verificar se o server_id já é master de uma partição
            //apenas queremos eleger leaders que não sejam master atualmente
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition2 in mapping)
            {
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition2)
                {
                    LinkedList<Server.PlaceholderServer> part_servers = entry.Value;
                    foreach(PlaceholderServer place_server in part_servers)
                    {
                        if (place_server.Server_id == server_id)
                        {
                            if (place_server.Is_master)
                            {
                                is_master = place_server.Is_master;
                                goto Finish;
                            }
                        }
                    }
                }
            }
            Finish:
            return is_master;
        }

        private string getMaxServerID(List<string> servers)
        {
            Console.WriteLine($"Comprimento da lista de input: {servers.Count}");
            string leader = "";
            foreach(string server in servers)
            {
                Console.WriteLine($"Item: {server}");
                if (leader == "")
                {
                    Console.WriteLine("aqui 1");
                    leader = server;
                }
                else
                {
                    //split para ver se o índice do server é maior do que o outro. Ex (s3 > s2)
                    char[] server_info = server.ToCharArray();
                    int a = (int)(server[1]-'0');
                    Console.WriteLine($"Valor de a: {a}");
                    int b = (int)(leader.ToCharArray()[1]-'0');
                    Console.WriteLine($"Valor de b: {b}");
                    if (a > b)
                    {
                        Console.WriteLine("aqui 2");
                        leader = server;
                    }
                }
            }
            return leader;
        }

        private List<string> getListServerID(string part_id)
        {
            List<string> serv_list = new List<string>();
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    if (entry.Key == part_id)
                    {
                        LinkedList<Server.PlaceholderServer> place_servers = entry.Value;
                        foreach(PlaceholderServer place_server in place_servers)
                        {
                            serv_list.Add(place_server.Server_id);
                        }
                    }
                }
            }
          return serv_list;
        }


        //Unlocks the objects locked under write operations, for a given partition
        private void UnlockObjects(string partition_id)
        {
            foreach(string object_id in this.objects_locked_write)
            {
                foreach(DataStoreObject obj in this.saved_objects)
                {
                    if (obj.Object_id == object_id && obj.Partition_id==partition_id)
                    {
                        obj.Is_locked_object = false;
                    }
                }
            }
        }

        public override Task<CrashedReplicaServerACK> RemoveCrashedReplicaServer(CrashedReplicaServer request, ServerCallContext context)
        {
            Console.WriteLine("*********************************************************************************");
            Console.WriteLine($"Server {this.server_id} is removing the crashed replica...");
            Console.WriteLine("*********************************************************************************");
            return Task.FromResult(RemoveCrashedReplicaServer_aux(request));
        }

        public CrashedReplicaServerACK RemoveCrashedReplicaServer_aux(CrashedReplicaServer request)
        {
            int partition_index = 0;
            List<int> replicas_to_erase_index = new List<int>();
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    if (entry.Key == request.Partitionid)
                    {
                        //refers to the index of the crashed server inside the current partition mapping
                        //to facilitate the deletion afterwards
                        int index_inside_partition = 0;

                        //partition where the replicas have crashed
                        
                        foreach (PlaceholderServer serv in entry.Value)
                        {
                            if (request.Serverid.Contains(serv.Server_id))
                            {
                                replicas_to_erase_index.Add(index_inside_partition);
                            }
                            index_inside_partition = index_inside_partition + 1;
                        }
                    }
                }
                partition_index = partition_index + 1;
            }

            //proper deletion of the replicas
            foreach (int index in replicas_to_erase_index)
            {
                Dictionary<string, LinkedList<Server.PlaceholderServer>>.ValueCollection a = this.mapping.ElementAt(partition_index).Values;
                PlaceholderServer aux = a.ElementAt(0).ElementAt(index);
                a.ElementAt(0).Remove(aux);
            }
            return new CrashedReplicaServerACK { };
        }

        /***********************************************************************************************************************/



        /*Getters and setters*/
        public string Server_id
        {
            get { return this.server_id; }
        }

        public string Url
        {
            get { return this.url; }
        }

        public int Min_delay
        {
            get { return this.min_delay; }
        }

        public int Max_delay
        {
            get { return this.max_delay; }
        }

        public List<(string, bool)> Responsability_map
        {
            get { return this.responsability_map; }
            set { this.responsability_map = value; }
        }

        public LinkedList<DataStoreObject> Saved_objects
        {
            get { return this.saved_objects; }
            set { this.saved_objects = value; }
        }

        public LinkedList<Dictionary<string, LinkedList<Server.PlaceholderServer>>> Mapping
        {
            set { this.mapping = value; }
        }
    }
}