using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SamagotchiServer
{
    public class SamagotchiServer
    {
        private const int ConnetionTimeout = 60;
        private const int Port = 13000;
        private const string ServerAddress = "127.0.0.1";

        private static TcpListener _server;
        private static readonly IList<Client> ClientPool = new List<Client>();

        private static bool _isRunning;

        private static void Main(string[] args)
        {
            Console.Title = "Samagotchi Server";
            CommandArgParser.From(args);
            var serverAddress = CommandArgParser.Value("ip") ?? ServerAddress;
            var port = CommandArgParser.Value("port") != null ? int.Parse(CommandArgParser.Value("port")) : Port;

            try
            {
                _server = new TcpListener(IPAddress.Parse(serverAddress), port);
                _server.Start();
                _isRunning = true;

                Console.WriteLine($"Server listing on {serverAddress}:{port}");

                Parallel.Invoke(LoopClients, GetInput, CheckConnetions);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                _server?.Stop();
            }
        }

        private static void CheckConnetions()
        {
            Console.WriteLine("Check Connections activated!");
            while (_isRunning)
            {
                foreach (var client in ClientPool.Where(client => client.DateTime.AddSeconds(ConnetionTimeout) < DateTime.Now))
                {
                    try
                    {
                        client.TcpClient.GetStream().Close();
                        client.TcpClient.Close();
                        client.Thread.Abort();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"Killed {client.Id} connection. Connected {client.TcpClient.Connected}");
                        Console.ResetColor();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                for (var i = 0; i < ClientPool.Count; i++)
                    if (ClientPool[i].TcpClient.Connected == false)
                        ClientPool.RemoveAt(i);

                Thread.Sleep(1000);
            }
        }

        private static void GetInput()
        {
            Console.WriteLine("Listening to user input!");
            string line;
            while (!string.IsNullOrEmpty(line = Console.ReadLine()) && _isRunning)
            {
                switch (line.ToLower())
                {
                    case "clients":
                        Console.WriteLine($"Connected Clients {ClientPool.Count}");
                        foreach (var client in ClientPool)
                            Console.WriteLine($"\t{client.Id}:{client.TcpClient.Connected}");
                        break;
                    case "exit":
                        _isRunning = false;
                        KillClients();
                        break;
                    default:
                        Console.WriteLine($"Command > {line}");
                        break;
                }
            }
        }

        private static void LoopClients()
        {
            Console.WriteLine("Client loop activated!");
            while (_isRunning)
            {
                var client = new Client { TcpClient = _server.AcceptTcpClient() };
                ClientPool.Add(client);
                client.StartThread();
            }
        }

        private static void KillClients()
        {
            for (var i = 0; i < ClientPool.Count; i++)
                if (ClientPool[i].TcpClient.Connected == false)
                {
                    var client = ClientPool[i];
                    client.TcpClient.GetStream().Close();
                    client.TcpClient.Close();
                    client.Thread.Abort();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Killed {client.Id} connection. Connected {client.TcpClient.Connected}");
                    Console.ResetColor();
                    ClientPool.RemoveAt(i);
                }
        }
    }
}
