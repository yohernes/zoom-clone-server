using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    class Program
    {
        const int portNo = 1500;
        private const string ipAddress = "10.27.201.150";

        static void Main(string[] args)
        {
           
            

            System.Net.IPAddress localAdd = System.Net.IPAddress.Parse(ipAddress);

            TcpListener listener = new TcpListener(localAdd, portNo);

            Console.WriteLine("Simple TCP Server");
            Console.WriteLine("Listening to ip {0} port: {1}", ipAddress, portNo);
            Console.WriteLine("Server is ready.");

            // Start listen to incoming connection requests
            listener.Start();

            // infinit loop.
            while (true)
            {
                // AcceptTcpClient - Blocking call
                // Execute will not continue until a connection is established

                // We create an instance of ChatClient so the server will be able to 
                // server multiple client at the same time.
                TcpClient tcp = listener.AcceptTcpClient();
                if (tcp != null)
                {
                    Thread t = new Thread(() => StartClient(tcp));
                    //Thread t = new Thread(new ThreadStart(StartClient),tcp);
                    t.Start();
                }

            }
        }
        public static void StartClient(TcpClient tcp)
        {
            IPEndPoint remoteEndPoint = tcp.Client.RemoteEndPoint as IPEndPoint;
            string clientIPAddress = remoteEndPoint.Address.ToString();
            int clientPort = remoteEndPoint.Port;

            Console.WriteLine("Connected to client at IP: {0}, Port: {1}", clientIPAddress, clientPort);

            Client user = new Client(tcp);
        }
    }
}
