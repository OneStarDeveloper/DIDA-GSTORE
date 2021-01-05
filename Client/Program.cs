using Grpc.Net.Client;
using Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Client main...");
            string username = args[0];
            Console.WriteLine($"Username: {username}");
            string url = args[1];
            Console.WriteLine($"URL: {url}");
            string script_file = args[2];
            Console.WriteLine($"Script file: {script_file}");

            Client client = new Client(username, url, script_file);
            Console.WriteLine("Starting client script...");
            client.StartScript();
            
        }
    }
}
