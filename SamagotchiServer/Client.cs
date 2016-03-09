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
            DateTime = DateTime.Now;
            Thread = new Thread(HandleClient);
            Thread.Start();
        }

        private void HandleClient()
        {
            var streamReader = new StreamReader(TcpClient.GetStream(), Encoding.ASCII);

            while (IsConnected())
            {
                try
                {
                    var line = streamReader.ReadLine();
                    var input = new ParseInput(line);
                    DateTime = DateTime.Now;

                    if (input.Command == "Connect")
                    {
                        Id = input.Guid;
                        Console.WriteLine($"Client Connected: {Id}");
                    }
                    else if (Id == input.Guid)
                    {
                        switch (input.Command)
                        {
                            case "EndConnection":
                                TcpClient.Close();
                                break;
                            default:
                                throw new InvalidCommand("Command does not exist");
                        }
                    }
                }
                catch (InvalidCommand exception)
                {
                    Console.WriteLine(exception.Message);
                }
                catch (Exception)
                {
                    TcpClient.Close();
                }
            }
            if (!Guid.Empty.Equals(Id))
                Console.WriteLine($"Client > {Id} Closed Connection");
        }

        private bool IsConnected()
        {
            try
            {
                var socket = TcpClient.Client;
                if (!socket.Connected) return false;

                if (!socket.Poll(0, SelectMode.SelectWrite) || socket.Poll(0, SelectMode.SelectError)) return false;

                var buffer = new byte[1];
                return socket.Receive(buffer, SocketFlags.Peek) != 0;

                //https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.poll.aspx
                //This method cannot detect certain kinds of connection problems, such as a broken network cable, or that the remote host was shut down ungracefully. You must attempt to send or receive data to detect these kinds of errors.
            }
            catch (SocketException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
    }

    public class ParseInput
    {
        public Guid Guid { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }

        public ParseInput(string input)
        {
            Guid guid;
            var parts = input.Split('|');
            var hasGuid = Guid.TryParse(parts[0], out guid);
            if (!hasGuid)
                throw new InvalidCommand("Incorrect Guid!");
            Guid = guid;

            if(parts.Length < 2)
                throw new InvalidCommand("Missing Command!");
            Command = parts[1];

            if (parts.Length == 3)
                Data = parts[2];
        }
    }

    public class InvalidCommand : Exception
    {
        public InvalidCommand()
        {
        }

        public InvalidCommand(string message) : base(message)
        {
        }

        public InvalidCommand(string message, Exception inner) : base(message, inner)
        {
        }
    }
}