using Client;
using Grpc.Core;
using Grpc.Net.Client;
using Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GStoreServices = Server.GStoreServices;

namespace PuppetMaster
{

    // PuppetList class
    public class PuppetList : PMServices.PMServicesBase
    {
        private List<Server> servList;
        private List<Partition> partList;
        private List<Client> cliList;


        public List<Server> getServList()
        {
            return this.servList;
        }
        public List<Client> getCliList()
        {
            return this.cliList;
        }
        public List<Partition> getPartList()
        {
            return this.partList;
        }

        public PuppetList()
        {
            Console.WriteLine("PM List...");
            servList = new List<Server>();
            partList = new List<Partition>();
            cliList = new List<Client>();
            new Thread(new ThreadStart(startPM)).Start();
        }

        // DelServer
        // - Funcao para remover um servidor da lista de servidores e matar um processo
        public void DelServer(string id)
        {
            Console.WriteLine($"Terminating Server {id} process....");
            Server serv = servList.Find(item => item.getID() == id);

            // Se nao encontrar o servidor, não faz nada
            if (serv == null)
            {
                MessageBox.Show("Server not found, please type the ID of an existing server.");
            }
            else
            {
                // Recebe o ID do processo do servidor em causa, faz kill do processo e remove o servidor da lista
                Process pro_serv = Process.GetProcessById(serv.getProID());
                pro_serv.Kill();
                servList.Remove(serv);
            }
        }


        //Add server info from GUI
        public void AddGUIServer(string server_id, string url, string min, string max)
        {
            //add server to server list
            this.servList.Add(new Server(server_id,url,Convert.ToInt32(min),Convert.ToInt32(max)));

            //launch server
            LaunchServer(server_id,url,min,max);

            //send partition to server
            sendPartitionToServer(server_id);
        }

        private void LaunchServer(string server_id,string url_arg, string min_arg, string max_arg)
        {
            // Cria um novo processo para o servidor
            Process pro_server = new Process();

            // Executável do servidor
            string path = Directory.GetCurrentDirectory();
            string newPath = Path.GetFullPath(Path.Combine(path, @"..\..\..\..\..\"));
            pro_server.StartInfo.FileName = newPath + @"\Server\bin\Debug\netcoreapp3.1\Server.exe";

            //definições para abrir noutra janela
            pro_server.StartInfo.UseShellExecute = true;
            pro_server.StartInfo.CreateNoWindow = false;
            pro_server.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            string id = server_id;
            string url = url_arg;
            string min = min_arg;
            string max = max_arg;
            List<(string, bool)> master_list = new List<(string, bool)>();

            //fill the master list with information regarding each server for each partition
            foreach (Partition p in this.partList)
            {
                foreach ((string s, bool b) in p.getServIDs())
                {
                    if (s == id)
                    {
                        //server we want
                        master_list.Add((p.getName(), b));
                    }
                }
            }

            string master_list_string = "";
            foreach ((string s, bool b) in master_list)
            {
                //append to string
                master_list_string = master_list_string + "(" + s + "," + b + ") ";
            }

            pro_server.StartInfo.Arguments = $"{id} {url} {min} {max} {master_list_string}";
            pro_server.Start();

            // Guarda o ID do Process criado para depois o poder eliminar
            foreach(Server server in this.servList)
            {
                if (server.getID() == server_id)
                {
                    server.setProID(pro_server.Id);
                }
            } 
        }



        // AddPartition     
        // - Funcao para adicionar uma particao a lista de particoes
        public void AddPartition(string r, string name, string str_servID)
        {
            Console.WriteLine($"Partição de input: {str_servID}");
            // Divide a string por virgulas e poe os elementos numa lista
            List<string> servIds_aux = str_servID.Split(' ').ToList();
            List<(string, bool)> servIds = new List<(string, bool)>();

            int count = 0;
            foreach (string server_id in servIds_aux)
            {
                if (server_id != "")
                {
                    Console.WriteLine($"Server in loop: {server_id}");
                    servIds.Add((server_id, count == 0));
                    count += 1;
                }
            }

            Partition partition = new Partition(Convert.ToInt32(r), name.Trim(), servIds);
            Console.WriteLine("»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»");
            Console.WriteLine("Final partition");
            Console.WriteLine($"Partition name: {partition.getName()}");
            foreach ((string s, bool b) in partition.getServIDs())
            {
                Console.WriteLine($"Server: {s}. Is master: {b}");
            }
            Console.WriteLine("»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»»");
            this.partList.Add(partition);
        }


        // LaunchClients     
        // - Funcao para arrancar todos os clientes
        public void LaunchClients()
        {
            Console.WriteLine("Vamos fazer launch dos clientes...");
            Console.WriteLine($"Comprimento da lista dos clientes: {this.cliList.Count} ");
            foreach (Client client in cliList)
            {
                // Cria um novo processo para o cliente
                Process pro_client = new Process();

                // Executável do cliente
                string path = Directory.GetCurrentDirectory();
                string newPath = Path.GetFullPath(Path.Combine(path, @"..\..\..\..\..\"));
                pro_client.StartInfo.FileName = newPath + @"\Client\bin\Debug\netcoreapp3.1\Client.exe";

                //definições para abrir noutra janela
                pro_client.StartInfo.UseShellExecute = true;
                pro_client.StartInfo.CreateNoWindow = false;
                pro_client.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                string username = client.getUsername();
                string url = client.getURL();
                string script = client.getScript();
                pro_client.StartInfo.Arguments = $"{username} {url} {script}.txt";
                pro_client.Start();

                client.setProID(pro_client.Id);

            }
        }



        // AddClient     
        // - Funcao para adicionar um cliente a lista de clientes
        public void AddClient(string username, string URL, string script)
        {
            // Cria o cliente e adiciona-o a lista
            Client client = new Client(username, URL, script);

            // Cria um novo processo para o cliente
            Process pro_client = new Process();

            // Guarda o ID do Process criado para depois o poder eliminar                
            string path = Directory.GetCurrentDirectory();
            string newPath = Path.GetFullPath(Path.Combine(path, @"..\..\..\..\..\"));
            pro_client.StartInfo.FileName = newPath + @"\Client\bin\Debug\netcoreapp3.1\Client.exe";

            //definições para abrir noutra janela
            pro_client.StartInfo.UseShellExecute = true;
            pro_client.StartInfo.CreateNoWindow = false;
            pro_client.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            // Passa a info dos servidores (por enquanto) 
            pro_client.StartInfo.Arguments = $"{username} {URL} {script}";
            pro_client.Start();

            client.setProID(pro_client.Id);
            cliList.Add(client);

            //enviar partição para o cliente
            sendPartitionToClient(username);
        }

        // DelClient     
        // - Funcao para apagar um cliente da lista de clientes
        public void DelClient(string username)
        {
            Client cli = cliList.Find(item => item.getUsername() == username);

            // Se nao encontrar o client, não faz nada
            if (cli == null)
            {
                MessageBox.Show("Client not found, please type the username of an existing client.");
            }
            else
            {
                // Recebe o ID do processo do cliente em causa, faz kill do processo e remove o cliente da lista
                Process pro_cli = Process.GetProcessById(cli.getProID());
                pro_cli.Kill();
                cliList.Remove(cli);
            }

        }

        // ----------------------------- FUNCOES PARA LEITURA DO SCRIPT INICIAL ----------------------------


        // AddClientInfo     
        // - Funcao para adicionar um cliente a lista de clientes
        public void AddClientInfo(string username, string URL, string script)
        {
            // Se passar a todas as verificacoes, cria o servidor e adiciona-o a lista
            Client client = new Client(username, URL, script);
            cliList.Add(client);
        }

        // AddServerInfo     
        // - Funcao para adicionar um servidor a lista de servidores
        public void AddServerInfo(string id, string URL, string MinDelay, string MaxDelay)
        {
            //só vamos adicionar servidor se ele não existir ainda na lista de servidores
            //iterar sobre as partições para preencher o campo boolean a indicar se o servidor é master ou não
            foreach (Partition p in this.partList)
            {
                foreach ((string server_id, bool master) in p.getServIDs())
                {
                    if (server_id == id && !existsServerInList(server_id))
                    {
                        Console.WriteLine("*********************************************************");
                        Console.WriteLine($"SERVIDOR A ADICIONAR: {id} - {URL}");
                        Console.WriteLine("*********************************************************");
                        Server server = new Server(id, URL, Convert.ToInt32(MinDelay), Convert.ToInt32(MaxDelay));
                        servList.Add(server);
                    }
                }
            }

        }

        public bool existsServerInList(string server_id)
        {
            bool exists_server = false;
            //iterar sobre a lista dos servidores
            foreach (Server s in this.servList)
            {
                if (s.getID() == server_id)
                {
                    //servidor já existe na lista
                    exists_server = true;
                    goto Finish;
                }
            }
        Finish:
            return exists_server;
        }



        // LaunchServers     
        // - Funcao para arrancar todos os servidores
        public void LaunchServers()
        {
            Console.WriteLine("Vamos fazer launch dos servers...");
            Console.WriteLine($"Comprimento da lista dos servidores: {this.servList.Count} ");
            foreach (Server server in servList)
            {
                // Cria um novo processo para o servidor
                Process pro_server = new Process();

                // Executável do servidor
                string path = Directory.GetCurrentDirectory();
                string newPath = Path.GetFullPath(Path.Combine(path, @"..\..\..\..\..\"));
                pro_server.StartInfo.FileName = newPath + @"\Server\bin\Debug\netcoreapp3.1\Server.exe";

                //definições para abrir noutra janela
                pro_server.StartInfo.UseShellExecute = true;
                pro_server.StartInfo.CreateNoWindow = false;
                pro_server.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                string id = server.getID();
                string url = server.getURL();
                string min = server.getMin().ToString();
                string max = server.getMax().ToString();
                List<(string, bool)> master_list = new List<(string, bool)>();

                //fill the master list with information regarding each server for each partition
                foreach (Partition p in this.partList)
                {
                    foreach ((string s, bool b) in p.getServIDs())
                    {
                        if (s == id)
                        {
                            //server we want
                            master_list.Add((p.getName(), b));
                        }
                    }
                }

                string master_list_string = "";
                foreach ((string s, bool b) in master_list)
                {
                    //append to string
                    master_list_string = master_list_string + "(" + s + "," + b + ") ";
                }

                pro_server.StartInfo.Arguments = $"{id} {url} {min} {max} {master_list_string}";
                pro_server.Start();

                // Guarda o ID do Process criado para depois o poder eliminar
                server.setProID(pro_server.Id);
            }
        }

        public void sendPartitionToProcesses()
        {
            Console.WriteLine("Sending partitions to everyone...");
            sendPartitionToServers();
            //sendPartitionToClients();
        }

        private void sendPartitionToServers()
        {
            Console.WriteLine("Sending the partition to the servers...");
#pragma warning disable CS0436 // Type conflicts with imported type
            CompletePartition complete_partition = new CompletePartition();
#pragma warning restore CS0436 // Type conflicts with imported type
            foreach (Partition p in this.partList)
            {
                Console.WriteLine($"Found partition: {p.getName()}");
#pragma warning disable CS0436 // Type conflicts with imported type
                PartitionInfo part = new PartitionInfo();
#pragma warning restore CS0436 // Type conflicts with imported type
                part.Partitionid = p.getName();
                foreach ((string s, bool b) in p.getServIDs())
                {
                    foreach (Server serv in this.servList)
                    {
                        if (serv.getID() == s)
                        {
                            Console.WriteLine($"Valur of server id: {serv.getID()}");
                            //add server info to message
#pragma warning disable CS0436 // Type conflicts with imported type
                            part.Servers.Add(new ServerInfo
#pragma warning restore CS0436 // Type conflicts with imported type
                            {
                                Serverid = s,
                                Url = serv.getURL(),
                                Mindelay = serv.getMin(),
                                Maxdelay = serv.getMax(),
                                Ismaster = b
                            });
                        }
                    }
                }
                complete_partition.Partitions.Add(part);
            }

            //send partition to every server
            foreach (Server serv in this.servList)
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(serv.getURL());
                GStoreServices.GStoreServicesClient client = new GStoreServices.GStoreServicesClient(channel);
                client.ReceivePartitionInfo(complete_partition);
            }
        }


        private void sendPartitionToServer(string server_id)
        {
            Console.WriteLine("Sending the partition to the server...");
#pragma warning disable CS0436 // Type conflicts with imported type
            CompletePartition complete_partition = new CompletePartition();
#pragma warning restore CS0436 // Type conflicts with imported type
            foreach (Partition p in this.partList)
            {
                Console.WriteLine($"Found partition: {p.getName()}");
#pragma warning disable CS0436 // Type conflicts with imported type
                PartitionInfo part = new PartitionInfo();
#pragma warning restore CS0436 // Type conflicts with imported type
                part.Partitionid = p.getName();
                foreach ((string s, bool b) in p.getServIDs())
                {
                    foreach (Server serv in this.servList)
                    {
                        if (serv.getID() == s)
                        {
                            Console.WriteLine($"Valur of server id: {serv.getID()}");
                            //add server info to message
#pragma warning disable CS0436 // Type conflicts with imported type
                            part.Servers.Add(new ServerInfo
#pragma warning restore CS0436 // Type conflicts with imported type
                            {
                                Serverid = s,
                                Url = serv.getURL(),
                                Mindelay = serv.getMin(),
                                Maxdelay = serv.getMax(),
                                Ismaster = b
                            });
                        }
                    }
                }
                complete_partition.Partitions.Add(part);
            }

            //send partition to every server
            foreach (Server serv in this.servList)
            {
                if (serv.getID() == server_id)
                {
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    GrpcChannel channel = GrpcChannel.ForAddress(serv.getURL());
                    GStoreServices.GStoreServicesClient client = new GStoreServices.GStoreServicesClient(channel);
                    client.ReceivePartitionInfo(complete_partition);
                } 
            }
        }

        private void sendPartitionToClient(string user_id)
        {
            Console.WriteLine("Sending partitions to the clients");
            var complete_partition = new CompletePartitionClient();

            foreach (Partition p in this.partList)
            {
                var part = new PartitionInfoClient();
                part.Partitionid = p.getName();
                foreach ((string s, bool b) in p.getServIDs())
                {
                    foreach (Server serv in this.servList)
                    {
                        if (serv.getID() == s)
                        {
                            //add server info to message
                            part.Servers.Add(new ServerInfoClient
                            {
                                Serverid = s,
                                Url = serv.getURL(),
                                Mindelay = serv.getMin(),
                                Maxdelay = serv.getMax(),
                                Ismaster = b
                            });
                        }
                    }
                }
                complete_partition.Partitions.Add(part);
            }

            //send partition to every client
            foreach (Client c in this.cliList)
            {
                if (c.getUsername() == user_id)
                {
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    GrpcChannel channel = GrpcChannel.ForAddress(c.getURL());
                    ClientServices.ClientServicesClient client = new ClientServices.ClientServicesClient(channel);
                    client.ReceivePartitionInfo(complete_partition);
                }
            }
        }

        public void propagateCrash(string server_id)
        {
            Console.WriteLine("*****************************************************");
            Console.WriteLine("Puppet Master is propagating crash...");
            Console.WriteLine("*****************************************************");
            //propagate to the servers
            foreach (Server s in this.servList)
            {
                Console.WriteLine($"Server {s.getID()}....");
                if (s.getID() != server_id)
                {
                    //do client to server and propagate
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    GrpcChannel channel = GrpcChannel.ForAddress(s.getURL());
                    var client = new GStoreServices.GStoreServicesClient(channel);
                    client.ServerCrasedServer(new ServerCrashedID
                    {
                        Serverid = server_id
                    });
                }
            }
            Console.WriteLine("Propagating to clients....");
            //propagate to the clients

            foreach (Client c in this.cliList)
            {
                //do client to server and propagate
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(c.getURL());
                var client = new ClientServices.ClientServicesClient(channel);
                client.ServerCrasedClient(new ServerCrashedClientID
                {
                    Serverclientid = server_id
                });
            }
        }

        public void startPM()
        {
            Console.WriteLine($"Starting Puppet Master in port 10000...............");
            Grpc.Core.Server pm_server = new Grpc.Core.Server
            {
                Services = { PMServices.BindService(this) },
                Ports = { new ServerPort("localhost", 10000, ServerCredentials.Insecure) }
            };

            pm_server.Start();
            Console.WriteLine($"Puppet Master is now listening...............");
            //Thread.Sleep(10000000);
            Console.ReadKey();
            Console.WriteLine($"Puppet Master is closing...............");
            pm_server.ShutdownAsync().Wait();
        }

        /////////////////////////////////////////

        /* grpc functions */
        public override Task<NewLeaderACK> UpdateLeader(NewLeader request, ServerCallContext context)
        {
            Console.WriteLine("***********************************************************************************************");
            Console.WriteLine("Puppet Master has received an update leader...");
            Console.WriteLine("***********************************************************************************************");
            return Task.FromResult(UpdateLeader_aux(request));
        }

        public NewLeaderACK UpdateLeader_aux(NewLeader request)
        {
            string partition_id = request.Partid;
            string leader_id = request.Leaderid;
            int pos_partlist = -1;
            int pos_servlist = -1;
            //iterate over partition and update
            foreach (Partition p in partList)
            {
                pos_partlist = pos_partlist + 1;
                if (p.getName() == partition_id)
                {
                    //partition to update
                    foreach ((string server, bool is_master) in p.getServIDs())
                    {
                        pos_servlist = pos_servlist + 1;
                        if (server == leader_id)
                        {
                            goto UpdateLead;
                        }
                    }
                }
            }
        UpdateLead:
            partList.ElementAt(pos_partlist).getServIDs().RemoveAt(pos_servlist);
            partList.ElementAt(pos_partlist).getServIDs().Add((leader_id, true));
            Console.WriteLine($"Puppet Master updated leader {leader_id} at partition {partition_id}");

            //propagate this change to clients
            Console.WriteLine("PuppetMaster is sending the update leader to the clients...");
            foreach (Client c in this.cliList)
            {
                //do client to server and propagate
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(c.getURL());
                var client = new ClientServices.ClientServicesClient(channel);
                client.UpdateLeaderClient(new LeaderInfo
                {
                    Partitionid = partition_id,
                    Leaderid = leader_id
                });
            }

            return new NewLeaderACK { };
        }




        public override Task<CrashedReplicaPMACK> RemoveCrashedReplicaPM(CrashedReplicaPM request, ServerCallContext context)
        {
            Console.WriteLine("*********************************************************************************");
            Console.WriteLine("PM is propagating the crashed replica...");
            Console.WriteLine("*********************************************************************************");
            return Task.FromResult(RemoveCrashedReplica_aux(request));
        }

        public CrashedReplicaPMACK RemoveCrashedReplica_aux(CrashedReplicaPM request)
        {
            //propagate to each server
            foreach (Server s in servList)
            {
                CrashedReplicaServer crashed_replica = new CrashedReplicaServer();
                crashed_replica.Partitionid = request.Partitionid;
                foreach (String serv_id in request.Serverid)
                {
                    crashed_replica.Serverid.Add(serv_id);
                }
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(s.getURL());
                var serv = new GStoreServices.GStoreServicesClient(channel);
                serv.RemoveCrashedReplica(crashed_replica);
            }

            //propagate to each client
            foreach (Client c in cliList)
            {
                CrashedReplicaClient crashed_replica = new CrashedReplicaClient();
                crashed_replica.Partitionid = request.Partitionid;
                foreach (String serv_id in request.Serverid)
                {
                    crashed_replica.Serverid.Add(serv_id);
                }
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(c.getURL());
                var client = new ClientServices.ClientServicesClient(channel);
                client.RemoveCrashedReplicaClient(crashed_replica);
            }
            return new CrashedReplicaPMACK { };
        }


        public string ShowAll()
        {
            string s = "";

            foreach (Server serv in servList)
            {
                s += "Server ID: " + serv.getID() + " | " +
                     "URL: " + serv.getURL() + " | " +
                     "Min Delay: " + serv.getMin() + " | " +
                     "Max Delay: " + serv.getMax() + "\r\n";
            }

            foreach (Partition part in partList)
            {
                s += "Partition Name: " + part.getName() + " | " + "Server ID's: ";

                foreach ((string server_id, bool is_master) in part.getServIDs())
                {
                    s += server_id + " ";
                }

                s += "\r\n";
            }

            foreach (Client cli in cliList)
            {
                s += "Client username: " + cli.getUsername() + " | " +
                     "URL: " + cli.getURL() + " | " +
                     "Script file: " + cli.getScript() + "\r\n";
            }

            return s;
        }

    }


    // Server class
    public class Server
    {
        private string id;
        private int pro_id;
        private string url;
        private int minDelay;
        private int maxDelay;

        public Server(string id, string url, int minDelay, int maxDelay)
        {
            this.id = id;
            this.url = url;
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
        }


        // Getters
        public string getID()
        {
            return this.id;
        }
        public string getURL()
        {
            return this.url;
        }
        public int getMax()
        {
            return this.maxDelay;
        }
        public int getMin()
        {
            return this.minDelay;
        }
        public int getProID()
        {
            return this.pro_id;
        }

        // Setters
        public void setID(string id)
        {
            this.id = id;
        }
        public void setURL(string URL)
        {
            this.url = URL;
        }
        public void setMin(int min)
        {
            this.minDelay = min;
        }
        public void setMax(int max)
        {
            this.maxDelay = max;
        }
        public void setProID(int pro_id)
        {
            this.pro_id = pro_id;
        }

    }


    // Partition class
    public class Partition
    {
        private int num_rep;
        private string name;
        private List<(string, bool)> server_ids;

        public Partition(int num_rep, string name, List<(string, bool)> server_ids)
        {
            this.num_rep = num_rep;
            this.name = name;
            this.server_ids = server_ids;
        }

        // Getters
        public int getNumRep()
        {
            return this.num_rep;
        }
        public string getName()
        {
            return this.name;
        }
        public List<(string, bool)> getServIDs()
        {
            return this.server_ids;
        }

        // Setters
        public void setNumRep(int num_rep)
        {
            this.num_rep = num_rep;
        }
        public void setName(string name)
        {
            this.name = name;
        }
        public void setServIDs(List<(string, bool)> list)
        {
            this.server_ids = list;
        }

    }

    // Client class
    public class Client
    {
        private string username;
        private int pro_id;
        private string url;
        private string script;

        public Client(string username, string url, string script)
        {
            this.username = username;
            this.url = url;
            this.script = script;
        }

        // Getters
        public string getUsername()
        {
            return this.username;
        }
        public string getURL()
        {
            return this.url;
        }
        public string getScript()
        {
            return this.script;
        }
        public int getProID()
        {
            return this.pro_id;
        }

        // Setters
        public void setUsername(string username)
        {
            this.username = username;
        }
        public void setURL(string URL)
        {
            this.url = URL;
        }
        public void setScript(string script)
        {
            this.script = script;
        }
        public void setProID(int pro_id)
        {
            this.pro_id = pro_id;
        }

    }
}
