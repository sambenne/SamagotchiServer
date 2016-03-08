using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SamagotchiServer
{
    public class SamagotchiServer
    {
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
            var dateTime = DateTime.Now;
            while (_isRunning)
            {
                foreach (var client in _clientPool.Where(client => client.DateTime.AddSeconds(5) < dateTime))
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
                dateTime = DateTime.Now;
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
            
        }
    }

    public class Client
    {
        public Guid Id { get; set; }
        public DateTime DateTime { get; set; }
        public TcpClient TcpClient { get; set; }
        public Thread Thread { get; set; }

        public void StartThread()
        {
            Id = Guid.NewGuid();
            DateTime = DateTime.Now;
            Thread = new Thread(HandleClient);
            Thread.Start();
        }

        private void HandleClient()
        {
            var streamReader = new StreamReader(TcpClient.GetStream(), Encoding.ASCII);

            while (TcpClient.Connected)
            {
                try
                {
                    var line = streamReader.ReadLine();
                    DateTime = DateTime.Now;
                    Console.WriteLine("Client > " + line);

                    if (line == "EndConnection")
                        TcpClient.Close();
                }
                catch (Exception)
                {
                    TcpClient.Close();
                }
            }
            Console.WriteLine($"Client > {Id} Closed Connection");
        }
    }
}
