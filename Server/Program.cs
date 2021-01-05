using System;
using System.Collections.Generic;
using Grpc.Core;
using Grpc.Net.Client;

namespace Server
{

    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Server main....");
            string server_id = args[0];
            Console.WriteLine($"Server ID: {server_id}");
            string url = args[1];
            Console.WriteLine($"Server URL: {url}");
            int min_delay = Convert.ToInt32(args[2]);
            Console.WriteLine($"Server min delay: {min_delay}");
            int max_delay = Convert.ToInt32(args[3]);
            Console.WriteLine($"Server max delay: {max_delay}");
            string master_list = args[4];

            //parse the list
            //input format: (p1,false) (p2,true)
            List<(string, bool)> is_master = new List<(string, bool)>();
            string[] master_list_split = master_list.Split(" ");
            foreach(string s in master_list_split)
            {
                //append to list of master
                string[] part_map_split = s.Split(",");
                string part = part_map_split[0].Replace("(","");
                //boolean to represent whether the current server in the string is a master
                bool masterhood = bool.Parse(part_map_split[1].Replace(")", ""));
                Console.WriteLine($"Partição do servidor {server_id}: {part}");
                Console.WriteLine($"Servidor {server_id} master da partição {part}: {masterhood}");
                is_master.Add((part,masterhood));
            }

            CustomServer cs = new CustomServer(server_id,url,min_delay,max_delay,is_master);
            Console.WriteLine($"Going to start Server {server_id}...");
            cs.startServer();
        }
    }
}
