using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

/* Note: If you are using .net core 2.1, you need to install System.Text.Json (use NuGet). */

namespace SocketClient
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class ClientInfo
    {
        public string studentnr { get; set; }
        public string classname { get; set; }
        public int clientid { get; set; }
        public string teamname { get; set; }
        public string ip { get; set; }
        public string secret { get; set; }
        public string status { get; set; }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class Message
    {
        public const string welcome = "WELCOME";
        public const string stopCommunication = "COMC-STOP";
        public const string statusEnd = "STAT-STOP";
        public const string secret = "SECRET";
    }

    public class Client
    {
        private Socket _clientSocket;
        private readonly ClientInfo _info;
        private IPEndPoint _localEndPoint;
        private readonly IPAddress _ipAddress = IPAddress.Parse("127.0.0.1");
        private const int PortNumber = 11111;
        private const int MinWaitingTime = 50, MaxWaitingTime = 100;
        private readonly int _waitingTime;
        private const string BaseStdNumber = "0700";

        public Client(bool finishing, int n)
        {
            _waitingTime = new Random().Next(MinWaitingTime, MaxWaitingTime);
            _info = new ClientInfo
            {
                classname = " DINF2 ",
                studentnr = BaseStdNumber + n,
                ip = "127.0.0.1",
                clientid = finishing ? -1 : 1
            };
        }

        private string GetClientInfo()
        {
            return JsonSerializer.Serialize(_info);
        }

        public void PrepareClient()
        {
            try
            {
                // Establish the remote endpoint for the socket.
                _localEndPoint = new IPEndPoint(_ipAddress, PortNumber);
                // Creation TCP/IP Socket using  
                _clientSocket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Client] Preparation failed: {0}", e.Message);
            }
        }

        private string ProcessMessage(string msg)
        {
            Console.WriteLine("[Client] from Server -> {0}", msg);
            var replyMsg = "";

            try
            {
                switch (msg)
                {
                    case Message.welcome:
                        replyMsg = GetClientInfo();
                        break;
                    default:
                        var c = JsonSerializer.Deserialize<ClientInfo>(msg);
                        if (c.status == Message.statusEnd)
                            replyMsg = Message.stopCommunication;
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Client] processMessage {0}", e.Message);
            }

            return replyMsg;
        }

        public void StartCommunication()
        {
            Console.Out.WriteLine("[Client] **************");
            Thread.Sleep(_waitingTime);
            // Data buffer 
            var messageReceived = new byte[1024];
            var stop = false;

            try
            {
                // Connect Socket to the remote endpoint 
                _clientSocket.Connect(_localEndPoint);
                // print connected EndPoint information  
                Console.WriteLine("[Client] connected to -> {0} ", _clientSocket.RemoteEndPoint);

                while (!stop)
                {
                    // Receive the message using the method Receive().
                    var numBytes = _clientSocket.Receive(messageReceived);
                    var rcvdMsg = Encoding.ASCII.GetString(messageReceived, 0, numBytes);
                    var reply = ProcessMessage(rcvdMsg);
                    SendReply(reply);
                    if (reply.Equals(Message.stopCommunication))
                        stop = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void SendReply(string msg)
        {
            // Create the message to send
            Console.Out.WriteLine("[Client] Message to be sent: {0}", msg);
            var messageSent = Encoding.ASCII.GetBytes(msg);
            _clientSocket.Send(messageSent);
        }

        public void EndCommunication()
        {
            Console.Out.WriteLine("[Client] End of communication to -> {0} ", _clientSocket.RemoteEndPoint);
            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.Close();
        }
    }

    public class ClientsSimulator
    {
        private readonly int _numberOfClients;
        private readonly Client[] _clients;
        private const int WaitingTimeForStop = 2000;


        public ClientsSimulator(int n)
        {
            _numberOfClients = n;
            _clients = new Client[_numberOfClients];
            for (var i = 0; i < _numberOfClients; i++)
            {
                _clients[i] = new Client(false, i);
            }
        }

        public void ConcurrentSimulation()
        {
            Console.Out.WriteLine("\n[ClientSimulator] Concurrent simulator is going to start ...");

            var threads = new ArrayList();
            for (var i = 0; i < _numberOfClients; i++)
            {
                var client = _clients[i];
                var clientNumber = i;

                var thread = new Thread(() =>
                {
                    Console.Out.WriteLine("Setting up client number {0}", clientNumber);
                    client.PrepareClient();
                    client.StartCommunication();
                    client.EndCommunication();
                });
                thread.Start();
                threads.Add(thread);
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            Console.Out.WriteLine("\n[ClientSimulator] All clients finished with their communications ... ");

            Thread.Sleep(WaitingTimeForStop);

            var endClient = new Client(true, -1);
            endClient.PrepareClient();
            endClient.StartCommunication();
            endClient.EndCommunication();
        }
    }

    internal static class Program
    {
        // Main Method 
        private static void Main()
        {
            Console.Clear();
            const int numberOfClients = 249;
            new ClientsSimulator(numberOfClients).ConcurrentSimulation();
        }
    }
}