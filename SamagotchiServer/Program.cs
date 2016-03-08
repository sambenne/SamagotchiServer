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
        private static IList<Client> _clientPool;

        private static bool _isRunning;

        private static void Main(string[] args)
        {
            _clientPool = new List<Client>();
            try
            {
                _server = new TcpListener(IPAddress.Parse(ServerAddress), Port);
                _server.Start();
                _isRunning = true;

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
            while (_isRunning)
            {
                foreach (var client in _clientPool.Where(client => client.DateTime.AddSeconds(ConnetionTimeout) < DateTime.Now))
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
                for (var i = 0; i < _clientPool.Count; i++)
                    if (_clientPool[i].TcpClient.Connected == false)
                        _clientPool.RemoveAt(i);

                Thread.Sleep(1000);
            }
        }

        private static void GetInput()
        {
            string line;
            while (!string.IsNullOrEmpty(line = Console.ReadLine()) && _isRunning)
            {
                switch (line.ToLower())
                {
                    case "clients":
                        Console.WriteLine($"Connected Clients {_clientPool.Count}");
                        foreach (var client in _clientPool)
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
            while (_isRunning)
            {
                var client = new Client { TcpClient = _server.AcceptTcpClient() };
                _clientPool.Add(client);
                client.StartThread();
            }
        }

        private static void KillClients()
        {
            for (var i = 0; i < _clientPool.Count; i++)
                if (_clientPool[i].TcpClient.Connected == false)
                {
                    var client = _clientPool[i];
                    client.TcpClient.GetStream().Close();
                    client.TcpClient.Close();
                    client.Thread.Abort();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Killed {client.Id} connection. Connected {client.TcpClient.Connected}");
                    Console.ResetColor();
                    _clientPool.RemoveAt(i);
                }
        }
    }
}
