/*** Fill these lines with your complete information.
 * Note: Incomplete information may result in FAIL.
 * Member 1: [First and Last name, first member]: Maarten de Goede
 * Member 2: [First name and Last name, second member]: Corné Visser
 * Std Number 1: [Student number of the first member] 0966770
 * Std Number 2: [Student number of the second member] 0965144
 * Class: [what is your class, example INF2C] DINF2
 ***/


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace SocketServer
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
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
    public static class Message
    {
        public const string welcome = "WELCOME";
        public const string stopCommunication = "COMC-STOP";
        public const string statusEnd = "STAT-STOP";
        public const string secret = "SECRET";
    }

    public class Server
    {
        private readonly Mutex _mutex = new Mutex();

        private Socket _listener;
        private IPEndPoint _localEndPoint;
        private readonly IPAddress _ipAddress = IPAddress.Parse("127.0.0.1");
        private const int PortNumber = 11111;

        private readonly LinkedList<ClientInfo> _clients = new LinkedList<ClientInfo>();

        private const int ListeningQueueSize = 500;

        public void PrepareServer()
        {
            var threads = new List<Thread>();

            try
            {
                Console.WriteLine("[Server] is ready to start ...");
                // Establish the local endpoint
                _localEndPoint = new IPEndPoint(_ipAddress, PortNumber);
                _listener = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Console.Out.WriteLine("[Server] A socket is established ...");
                // associate a network address to the Server Socket. All clients must know this address
                _listener.Bind(_localEndPoint);
                // This is a non-blocking listen with max number of pending requests
                _listener.Listen(ListeningQueueSize);

                while (true)
                {
                    Console.WriteLine("Waiting connection ... ");

                    var socket = _listener.Accept();
                    var thread = new Thread(() => HandleClient(socket));
                    thread.Start();
                    threads.Add(thread);
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
        }

        private void HandleClient(Socket socket)
        {
            SendReply(socket, Message.welcome);

            while (true)
            {
                var bytes = new byte[1024];

                var numByte = socket.Receive(bytes);
                var data = Encoding.ASCII.GetString(bytes, 0, numByte);
                var replyMsg = ProcessMessage(data);

                if (replyMsg.Equals(Message.stopCommunication))
                    break;
                SendReply(socket, replyMsg);
            }
        }

        private string ProcessMessage(string message)
        {
            Console.WriteLine("[Server] received from the client -> {0} ", message);

            try
            {
                if (message == Message.stopCommunication)
                    return Message.stopCommunication;

                var clientInfo = JsonSerializer.Deserialize<ClientInfo>(message);

                _mutex.WaitOne();
                _clients.AddLast(clientInfo);
                _mutex.ReleaseMutex();

                if (clientInfo.clientid == -1)
                    PrintClients();

                clientInfo.secret = clientInfo.studentnr + Message.secret;
                clientInfo.status = Message.statusEnd;
                return JsonSerializer.Serialize(clientInfo);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Server] processMessage {0}", e.Message);
                return "";
            }
        }

        private static void SendReply(Socket connection, string message)
        {
            connection.Send(Encoding.ASCII.GetBytes(message));
        }

        private void PrintClients()
        {
            const string delimiter = " , ";
            Console.Out.WriteLine("[Server] This is the list of clients communicated");
            foreach (var client in _clients)
                Console.WriteLine(client.classname + delimiter + client.studentnr + delimiter + client.clientid);

            Console.Out.WriteLine("[Server] Number of handled clients: {0}", _clients.Count);

            _clients.Clear();
        }
    }

    internal static class Program
    {
        // Main Method 
        private static void Main()
        {
            Console.Clear();
            new Server().PrepareServer();
        }
    }
}