using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SamagotchiServer
{
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