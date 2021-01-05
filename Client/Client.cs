using Grpc.Core;
using Grpc.Net.Client;
using Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class Client:ClientServices.ClientServicesBase
    {
        private string username;
        private string url;
        private string script_file;
        private LinkedList<Dictionary<string,LinkedList<Server.PlaceholderServer>>> mapping = new LinkedList<Dictionary<string, LinkedList<PlaceholderServer>>>();

        //this attribute is here just to prevent the scripts from being executed before the reception of the partitions
        private bool is_partition_ready = false;
        
        //default server to be attached
        private GStoreServices.GStoreServicesClient default_attached = null;

        //list of crashed servers (to display when status message comes)
        //(server_id, partition_id)
        LinkedList<(string, string)> crashed_servers = new LinkedList<(string, string)>();

        public Client() { }

        public Client(string username, string url, string script_file)
        {
            this.username = username;
            this.url = url;
            this.script_file = script_file;
            new Thread(new ThreadStart(startClientServer)).Start();
        }

        public void startClientServer()
        {
            Console.WriteLine($"Client {this.username} is starting server.");
            Grpc.Core.Server server = new Grpc.Core.Server { 
                Services = { ClientServices.BindService(this) },
                Ports = {new ServerPort("localhost",Convert.ToInt32(this.url.Split(":")[2]),ServerCredentials.Insecure)}
            };
            server.Start();
            Console.WriteLine($"Client server {this.username} is now listening...............");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }

        //logic to run client script
        public void StartScript()
        {
            //só vamos processar o script se o cliente tiver recebido a partição
            Console.WriteLine("Client is checking is he has partition info...");
            while(!this.is_partition_ready)
            {
            }
            Console.WriteLine("Client has received the partition info!");
            string path = Directory.GetCurrentDirectory();
            string newPath = Path.GetFullPath(Path.Combine(path, @"..\..\..\..\..\"));
            string[] lines = System.IO.File.ReadAllLines(newPath + @"\Client\" + script_file);

            int count = 0;
            foreach (string line in lines)
            {
                string command = line.Split(' ')[0];
                switch (command)
                {
                    case "read":
                        if (!line.Contains("$"))
                        {
                            //only does normal reading if not inside loop (loop has variable $)
                            ProcessReads(line);
                        }
                        count += 1;
                        break;
                    case "write":
                        if (!line.Contains("$")){
                            //only does normal writing if not inside loop (loop has variable $)
                            ProcessWrites(line);
                        }
                        count += 1;
                        break;
                    case "listServer":
                        ProcessListServer(line);
                        count += 1;
                        break;
                    case "listGlobal":
                        ProcessListGlobal(line);
                        count += 1;
                        break;
                    case "wait":
                        ProcessWait(line);
                        count += 1;
                        break;
                    case "begin-repeat":
                        Console.WriteLine("Encontrei um begin-repeat");
                        Console.WriteLine($"Número de repetições: {line.Split(" ")[1]}");
                        List<string> instructions = new List<string>();
                        foreach(string s in lines.Skip(count+1))
                        {
                            Console.WriteLine($"Instruction in main method: {s}");

                            if (s == "end-repeat")
                            {
                                Console.WriteLine("Encontrei um end-repeat...");
                                goto EndFor;
                            }
                            Console.WriteLine($"Passei por aqui na instrução: {s}");
                            instructions.Add(s);
                        }
                        EndFor:
                            ProcessLoopArray(instructions, Convert.ToInt32(line.Split(' ')[1]));
                            Console.WriteLine("Instrução do begin-repeat acabou");
                            count += 1;
                            break;
                    case "end-repeat":
                        count += 1;
                        break;
                }
            }
        }

        private void ProcessLine(string line)
        {
            string command = line.Split(' ')[0];

            switch (command)
            {
                case "read":
                    ProcessReads(line);
                    break;
                case "write":
                    ProcessWrites(line);
                    break;
                case "listServer":
                    ProcessListServer(line);
                    break;
                case "listGlobal":
                    ProcessListGlobal(line);
                    break;
                case "wait":
                    ProcessWait(line);
                    break;
            }
        }
       
        private void ProcessWrites(string line)
        {
            Console.WriteLine("Write...");
            RepeatWrite:
            string[] split_string = line.Split(" ");
            //connection to master server for that partition
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            try
            {
                Console.WriteLine($"Master para a partição {split_string[1]}: {getMasterURL(split_string[1])}");
                string url_master = getMasterURL(split_string[1]);

                if (url_master!="")
                {
                    GrpcChannel channel = GrpcChannel.ForAddress(url_master);
                    var client = new GStoreServices.GStoreServicesClient(channel);

                    var final_content = "";

                    for (int i = 3; i != split_string.Length; i++)
                    {
                        final_content += " " + split_string[i];
                    }

                    Console.WriteLine($"Cliente {this.username} vai escrever o seguinte: {final_content}");

                    //ask server to perform write method
                    var result = client.Write(new WriteObjectClient
                    {
                        Partid = split_string[1],
                        Objectid = split_string[2],
                        Object = final_content.Trim()
                    });
                }
                else
                {
                    //significa que o master da partição não existe
                    throw new Exception("Master server not found...");
                }
            }
            catch
            {
                //master está em baixo
                //algoritmo de escolha de líder provavelmente a ser corrido pelos servidores
                //vamos fazer novamente o processo para verificar se existe um novo master que surgiu
                Console.WriteLine("***************************************************************************************");
                Console.WriteLine($"Master da partição {split_string[1]} está em baixo para a escrita...");
                Console.WriteLine("Vamos repetir o processo de escrita...");
                Console.WriteLine("***************************************************************************************");
                goto RepeatWrite;
            }
        }

        private void ProcessReads(string line)
        {
            //check if client is attached to a default server
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            Console.WriteLine("Isto foi uma leitura...");

            RepeatRead:
            string[] split_string = line.Split(" ");
            
            //client doesn't have a default server
            //randomly choose server from partition that has the object
            if (this.default_attached == null)
            {
                //there is no server attached currently
                Console.WriteLine("There is no server attached currently");

                //attach to server that we are reading the object from
                generateDefaultServer(line);
            }

            try
            {
                //ask the server if he serves that object
                bool has_object = this.default_attached.HasObject(new HasObjectMsg
                {
                    Partid = split_string[1],
                    Objectid = split_string[2]
                }).Ok;

                if (has_object)
                {
                    Console.WriteLine("Servidor tem o objeto...");
                    //server has object
                    //ask server to perfom Read method
                    var result = default_attached.Read(new ReadObjectClient
                    {
                        Partid = split_string[1],
                        Objectid = split_string[2]
                    });
                    Console.WriteLine("******************************");
                    Console.WriteLine("Read operation result");
                    Console.WriteLine($"Object value: {result.Objectclient}");
                    Console.WriteLine("******************************");
                }
                else
                {
                    Console.WriteLine("Servidor atual não tem objeto...");
                    //attached server does not have the object
                    //attach to the server_id argument
                    if (split_string[3] != "-1")
                    {
                        Console.WriteLine($"Client {this.username} is going to attach to the server sent on the script...");
                        attachToServer(line);
                        var result = default_attached.Read(new ReadObjectClient
                        {
                            Partid = split_string[1],
                            Objectid = split_string[2]
                        });
                        Console.WriteLine("******************************");
                        Console.WriteLine("Read operation result");
                        Console.WriteLine($"Object value: {result.Objectclient}");
                        Console.WriteLine("******************************");
                    }
                    else
                    {
                        Console.WriteLine($"Client {this.username} found -1 on the script...");
                        Console.WriteLine($"Client {this.username} is going to find out where the object is...");
                        //server_id is set to -1
                        //in spite being -1, the object might exist in the partition
                        //iterate through every server from the partition and asks if he has the object
                        //if no one has it, return N/A
                        bool has_found = false;
                        foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in this.mapping)
                        {
                            if (partition.ContainsKey(split_string[1]))
                            {
                                LinkedList<Server.PlaceholderServer> server_list = partition[split_string[1]];
                                foreach (PlaceholderServer place_server in server_list)
                                {
                                    if (!has_found)
                                    {
                                        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                        GrpcChannel channel = GrpcChannel.ForAddress(place_server.Url);
                                        GStoreServices.GStoreServicesClient aux_server = new GStoreServices.GStoreServicesClient(channel);
                                        has_found = aux_server.HasObject(new HasObjectMsg
                                        {
                                            Partid = split_string[1],
                                            Objectid = split_string[2]
                                        }).Ok;

                                        if (has_found)
                                        {
                                            //ask for the object
                                            var result = aux_server.Read(new ReadObjectClient
                                            {
                                                Partid = split_string[1],
                                                Objectid = split_string[2]
                                            });
                                            Console.WriteLine("******************************");
                                            Console.WriteLine("Read operation result");
                                            Console.WriteLine($"Object value: {result.Objectclient}");
                                            Console.WriteLine("******************************");
                                            goto TheEnd;
                                        }
                                    }
                                TheEnd:
                                    break;
                                }
                            }
                        }
                        if (!has_found)
                        {
                            //object not found in that partition
                            Console.WriteLine("N/A");
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("*****************************************************************************************");
                Console.WriteLine("The server you are trying to read from is down...");
                Console.WriteLine("Repeating the read process again...");
                this.default_attached = null;
                goto RepeatRead;
                Console.WriteLine("*****************************************************************************************");
            }
            
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
        }

        private void ProcessListServer(string line)
        {
            Console.WriteLine("*******************************************************");
            Console.WriteLine("Isto foi um listServer");
            Console.WriteLine("*******************************************************");
            string server_id = line.Split(" ")[1];
            bool has_send = false;
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    var servers = entry.Value;
                    foreach (PlaceholderServer place_server in servers)
                    {
                        if (place_server.Server_id == server_id && !has_send)
                        {
                            has_send = true;
                            //get server url
                            string url_script = place_server.Url;
                            //connect to server
                            try
                            {
                                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                GrpcChannel channel = GrpcChannel.ForAddress(url_script);
                                var client = new GStoreServices.GStoreServicesClient(channel);
                                //grpc to request server objects
                                var request_objects = client.RequestAllObjects(new RequestAllObj {
                                    PartId = entry.Key
                                });

                                Console.WriteLine($"Output do Servidor {place_server.Server_id}:");
                                foreach (AllPartInfo part in request_objects.Partitions)
                                {
                                    Console.WriteLine($"\tPartição do servidor: {part.PartId}");
                                    Console.WriteLine($"\tServidor master da partição?: {part.Ismaster}");
                                    Console.WriteLine($"\tObjetos guardados nesta partição pelo servidor:");
                                    foreach (ObjectInfo obj in part.Objects)
                                    {
                                        Console.WriteLine($"\t\tID do objeto: {obj.Objectid}, Valor guardo no objeto: {obj.Objectvalue}");
                                    }
                                }
                            }
                            catch{
                                Console.WriteLine("***************************************************************************");
                                Console.WriteLine($"The server you tried to reach ({place_server.Server_id}) for the listServer is down...");
                                Console.WriteLine("***************************************************************************");
                            }
                        }
                    }
                }
            }
        }

        private void ProcessListGlobal(string line)
        {
            Console.WriteLine("This was a listGlobal...");
            Console.WriteLine("listServer for each server in the mapping...");
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    Console.WriteLine("*******************************************************************************");
                    Console.WriteLine($"Partition ID: {entry.Key}");
                    var object_id = new List<string>();
                    foreach (PlaceholderServer place_server in entry.Value)
                    {
                        ProcessListServer("listServer "+place_server.Server_id);
                    }
                    Console.WriteLine("*******************************************************************************");
                }
            }
        }

        //generates a default attached server to the client~, when there is none
        private void generateDefaultServer(string line)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            string url = getServerURL(line);
            if (url == "")
            {
                //partição vazia
                default_attached = null;
            }
            else
            {
                Console.WriteLine($"This is the server chosen for the default server: {url}");
                GrpcChannel channel = GrpcChannel.ForAddress(url);
                default_attached = new GStoreServices.GStoreServicesClient(channel);
            }   
        }
        
        //generates a url for the case when there is no current default attached servers to the client
        private string getServerURL(string script_line)
        {
            string[] split_line = script_line.Split(" ");
            PlaceholderServer random_server = null;
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in this.mapping)
            {
                if (partition.ContainsKey(split_line[1]))
                {
                    Begin:
                    int server_count = partition[split_line[1]].Count;
                    Random rnd = new Random();
                    int index = rnd.Next(0,server_count);
                    random_server = partition[split_line[1]].ElementAt(index);
                    if (random_server.Is_master)
                    {
                        goto Begin;
                    }
                }
            }
            return random_server.Url;
        }

        //retrieve the master from the partition passed as argument
        private string getMasterURL(string partition_id)
        {
            PlaceholderServer random_server = null;
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in this.mapping)
            {
                if (partition.ContainsKey(partition_id))
                {
                    foreach(PlaceholderServer place_server in partition[partition_id])
                    {
                        if (place_server.Is_master)
                        {
                            random_server = place_server;
                        }
                    }
                }
            }

            if (random_server==null)
            {
                return "";
            }
            return random_server.Url;
        }

        //attach to a specific server
        private void attachToServer(string line)
        {
            string[] split_line = line.Split(" ");
            PlaceholderServer current_server = null;
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in this.mapping)
            {
                if (partition.ContainsKey(split_line[1]))
                {
                    LinkedList<Server.PlaceholderServer> server_list = partition[split_line[1]];
                    foreach(PlaceholderServer place_server in server_list)
                    {
                        if(place_server.Server_id == split_line[3])
                        {
                            current_server = place_server;
                        }
                    }
                }
            }
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress(current_server.Url);
            default_attached = new GStoreServices.GStoreServicesClient(channel);
        }

        private void ProcessWait( string line)
        {
            int amount_wait = Convert.ToInt32(line.Split(" ")[1]);
            Console.WriteLine($"This is a Wait message. Client is now waiting for {amount_wait} milliseconds");
            Thread.Sleep(amount_wait);
        }

        private void ProcessLoopArray(List<string> instructions, int number_iterations)
        {
            Console.WriteLine("###############################################################################################");
            Console.WriteLine("Vamos processar as instruções dentro do loop");
            Console.WriteLine($"Número de vezes que vamos iterar: {number_iterations}");
            Console.WriteLine($"Número de instruções a executar: {instructions.Count}");
            for(int i = 0; i != number_iterations; i++)
            {
                foreach (string instruction in instructions)
                {
                    string s = instruction.Replace("$i", (i + 1).ToString());
                    ProcessLine(s);
                }
            }
            Console.WriteLine("Loop finished...");
            Console.WriteLine("###############################################################################################");
        }

        /***********************************************************************************************************************/
        //Implementing receiving partitions from Puppet Master

        public override Task<PartitionACK> ReceivePartitionInfo(CompletePartitionClient request, ServerCallContext context)
        {
            return Task.FromResult(ReceivePartitionInfo_aux(request));
        }

        public PartitionACK ReceivePartitionInfo_aux (CompletePartitionClient request)
        {
            Console.WriteLine("****************************************************************************************************");
            Console.WriteLine($"Client {this.username} is receiving the partition from PuppetMaster...");
            foreach (PartitionInfoClient partition in request.Partitions)
            {
                Dictionary<string, LinkedList<Server.PlaceholderServer>> part = new Dictionary<string, LinkedList<PlaceholderServer>>();
                string partition_id = partition.Partitionid;
                Console.WriteLine($"Partição recebida: {partition_id}");
                LinkedList<Server.PlaceholderServer> part_servers = new LinkedList<PlaceholderServer>();
                foreach (ServerInfoClient server in partition.Servers)
                {
                    Console.WriteLine($"Servidor da partição {partition_id}: {server.Serverid}");
                    part_servers.AddLast(new PlaceholderServer(server.Serverid,server.Url,server.Ismaster));
                }
                part.Add(partition_id,part_servers);
                this.mapping.AddLast(part);
            }
            this.is_partition_ready = true;
            Console.WriteLine("****************************************************************************************************");
            return new PartitionACK { };
        }

        /***********************************************************************************************************************/

        /***********************************************************************************************************************/
        //Implementing receiving status from puppet master
        public override Task<StatusClientAck> StatusClient(AskStatusClient request, ServerCallContext context)
        {
            return Task.FromResult(StatusClient_aux(request));
        }

        private StatusClientAck StatusClient_aux(AskStatusClient request)
        {
            Console.WriteLine("*************************************************");
            Console.WriteLine($"Nova mensagem de status recebida no cliente: {this.username}");
            Console.WriteLine($"Estado atual dos servidores no mapeamento:");
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    Console.WriteLine($"#Partition {entry.Key}:");
                    foreach (PlaceholderServer place_server in entry.Value)
                    {
                        Console.WriteLine($"\t -> Server {place_server.Server_id}, Up: {place_server.Status}, Master:{place_server.Is_master}");
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
            return new StatusClientAck { };
        }

        /***********************************************************************************************************************/

        /***********************************************************************************************************************/
        //implementing updates on the partition

        public override Task<ServerCrashedClientACK> ServerCrasedClient(ServerCrashedClientID request, ServerCallContext context)
        {
            Console.WriteLine("*************************************************");
            Console.WriteLine($"Some server is down. Client {this.username} is updating partition...");
            Console.WriteLine("*************************************************");
            return Task.FromResult(ServerCrashedClient_aux(request));
        }

        public ServerCrashedClientACK ServerCrashedClient_aux(ServerCrashedClientID request)
        {
            //refers to the position, of the server to be deleted, in the current partition
            int list_position = -1;

            List<(int, int)> to_delete = new List<(int, int)>();
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                list_position = list_position + 1;
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    //refers to the position of the partition of the deleted server, in the current mapping
                    int part_position = -1;
                    
                    foreach (PlaceholderServer serv in entry.Value)
                    {
                        part_position = part_position + 1;
                        if (serv.Server_id == request.Serverclientid)
                        {
                            //found the server to delete

                            //add to be deleted later
                            to_delete.Add((list_position, part_position));

                            //add crashed server to the client's crashed server list
                            this.crashed_servers.AddLast((serv.Server_id, entry.Key));

                            Console.WriteLine("*************************************************");
                            Console.WriteLine($"Client {this.username}: Just updated partition. The server deleted was: {serv.Server_id}");
                            Console.WriteLine("*************************************************");
                        }
                    }
                }
            }

            //iterate over the parts of the list to update
            foreach ((int part, int serv) in to_delete)
            {
                Dictionary<string, LinkedList<Server.PlaceholderServer>>.ValueCollection a = this.mapping.ElementAt(part).Values;
                PlaceholderServer aux = a.ElementAt(0).ElementAt(serv);
                
                // .ElementAt(0) is there because list a returns a list of the values we have (since we only have one value it is in the first position)
                a.ElementAt(0).Remove(aux);
            }

            return new ServerCrashedClientACK { };
        }

        /***********************************************************************************************************************/

        /***********************************************************************************************************************/
        //update leader

        public override Task<LeaderInfoACK> UpdateLeaderClient(LeaderInfo request, ServerCallContext context)
        {
            Console.WriteLine("*************************************************");
            Console.WriteLine("Client is about to update leader...");
            Console.WriteLine("*************************************************");
            return Task.FromResult(UpdateLeaderClient_aux(request));
        }

        private LeaderInfoACK UpdateLeaderClient_aux(LeaderInfo request)
        {
            //position in the mapping list where the partition of the new master is located
            int list_position = -1;

            //position in the servers list that the new master is located
            int server_part_pos = -1;

            //determine where is the new master located so we can update his information
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                list_position = list_position + 1;
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    if(entry.Key == request.Partitionid)
                    {
                        foreach (PlaceholderServer serv in entry.Value)
                        {
                            server_part_pos = server_part_pos + 1;
                            if (serv.Server_id == request.Leaderid)
                            {
                                goto FinishUpdate;
                            }
                        }
                    }
                }
            }
        FinishUpdate:
            lock (this)
            {
                //updating new master info in the partition
                this.mapping.ElementAt(list_position)[request.Partitionid].ElementAt(server_part_pos).Is_master = true;
            }
            Console.WriteLine("*************************************************");
            Console.WriteLine($"Client has updated the leader...");
            Console.WriteLine("*************************************************");

            return new LeaderInfoACK { };
        }

        public override Task<CrashedReplicaClientACK> RemoveCrashedReplicaClient(CrashedReplicaClient request, ServerCallContext context)
        {
            Console.WriteLine("*********************************************************************************");
            Console.WriteLine("Client is removing the crashed replica...");
            Console.WriteLine("*********************************************************************************");
            return Task.FromResult(RemoveReplicaClient_aux(request));
        }

        public CrashedReplicaClientACK RemoveReplicaClient_aux(CrashedReplicaClient request)
        {
            int partition_index = 0;
            List<int> replicas_to_erase_index = new List<int>();
            foreach (Dictionary<string, LinkedList<Server.PlaceholderServer>> partition in mapping)
            {
                foreach (KeyValuePair<string, LinkedList<Server.PlaceholderServer>> entry in partition)
                {
                    if (entry.Key == request.Partitionid)
                    {
                        int i = 0;
                        //partition where the replicas have crashed
                        List<string> servers_crashed = new List<string>();
                        foreach (String s in request.Serverid)
                        {
                            servers_crashed.Add(s);
                        }
                        foreach(PlaceholderServer serv in entry.Value)
                        {
                            if (servers_crashed.Contains(serv.Server_id))
                            {
                                replicas_to_erase_index.Add(i);
                            }
                            i = i + 1;
                        }
                    }
                }
                partition_index = partition_index + 1;
            }

            //proper delete the replicas
            foreach(int index in replicas_to_erase_index)
            {
                Dictionary<string, LinkedList<Server.PlaceholderServer>>.ValueCollection a = this.mapping.ElementAt(partition_index).Values;
                PlaceholderServer aux = a.ElementAt(0).ElementAt(index);
                a.ElementAt(0).Remove(aux);
            }

            return new CrashedReplicaClientACK { };
        }

        /***********************************************************************************************************************/

        /*Getters and setters section*/
        public string Username
        {
            get { return this.username; }
        }

        public string Url
        {
            get { return this.url; }
        }

        public string Script_file
        {
            get { return this.script_file; }
        }

    }
}
