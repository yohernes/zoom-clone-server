using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public class UdpServer
    {
        public static List<int> UsedPorts = new List<int>();
        public List<UdpClientInfo> clientInfo;
        private UdpClient udpServer;
        private CancellationTokenSource cts;
        public string meetingcode;



        private static readonly char[] AllowedCharacters =
      "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly HashSet<string> GeneratedCodes = new HashSet<string>();
        private static readonly Random Random = new Random();
        public static readonly Dictionary<string, UdpServer> codetoport = new Dictionary<string, UdpServer>();
        public int Port { get; private set; }
        
        

        public UdpServer()
        {
            Port = FindAvailablePort(12345, 20000);
            if (Port == 0)
            {
                Console.WriteLine("Too many connected ports!");
                Stop();
                return;
            }

            udpServer = new UdpClient(Port);
            clientInfo = new List<UdpClientInfo>();
            cts = new CancellationTokenSource();
            meetingcode = CreateMeeting();
        }

        public void AddClient(AesEncryption aes, string username, IPEndPoint iPEndPoint)
        {
            UdpClientInfo newClient = new UdpClientInfo(iPEndPoint, aes, username);
            clientInfo.Add(newClient);
            Console.WriteLine($"Client {newClient.name} has joined server {Port}");
            SendNewUser(newClient.name);

        }

        public void Start()
        {
            Console.WriteLine($"UDP server started. Port {this.Port} waiting for messages...");
            _ = ReceiveMessagesAsync(cts.Token);
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    UdpReceiveResult result = await udpServer.ReceiveAsync();
                    IPEndPoint clientEndPoint = result.RemoteEndPoint;
                    byte[] receivedBytes = result.Buffer;
                    string data = Encoding.UTF8.GetString(receivedBytes);

                    foreach (UdpClientInfo client in clientInfo)
                    {
                        if (!client.IPEndPoint.Equals(clientEndPoint))
                        {
                            continue;
                        }
                        Console.WriteLine($"Server number {Port} received message from {client.name} from ip {clientEndPoint}");
                        byte[] decbytes = client.aes.DecryptDatafr(receivedBytes);
                        var (metadata, Dataio) = GetMetadataAndData(decbytes, "|");
                        string[] metaarray = metadata.Split('#');
                        string datatype = metaarray[0];
                        switch (datatype)
                        {
                            case "%%%Frame%%%":
                                
                                foreach(UdpClientInfo clientim in clientInfo)
                                {
                                    if (!clientim.IPEndPoint.Equals(clientEndPoint))
                                    {
                                        byte[] frDataio = AddMetaData(Dataio, client.name, "Frame");
                                        byte[] encryptedframe = client.aes.EncryptFrameByte(frDataio, clientim.aes.Key, clientim.aes.IV);
                                        udpServer.Send(encryptedframe, encryptedframe.Length, clientim.IPEndPoint);
                                        Console.WriteLine($"sent frame from {clientEndPoint} to {clientim.IPEndPoint.ToString()}");
                                    }
                                }
                                break;

                            case "%%%Audio%%%":
                                
                            
                                foreach (UdpClientInfo clientim in clientInfo)
                                
                                {
                                
                                    if (clientim.IPEndPoint.Equals(clientEndPoint))
                                    {
                                        continue ;
                                    }

                                    byte[] frDataio = AddMetaData(Dataio, client.name, "Audio");
                                    byte[] encryptedframe = client.aes.EncryptAudio(frDataio, clientim.aes.Key, clientim.aes.IV);
                                    udpServer.Send(encryptedframe, encryptedframe.Length, clientim.IPEndPoint);
                                    Console.WriteLine($"sent audio from {clientEndPoint} to {clientim.IPEndPoint.ToString()}");

                                };
                                
                                break;
                                

                                
                            case "%%%StopCamera%%%":

                            
                                foreach (UdpClientInfo clientim in clientInfo)
                                
                                {
                                
                                    if (clientim.IPEndPoint.Equals(clientEndPoint))
                                    {
                                        continue;
                                    }

                                    string message = $"%%%StopCamera%%%#{client.name}|";

                                    byte[] dataio = Encoding.ASCII.GetBytes(message);

                                    EncryptSendByte(dataio, clientim);


                                };
                                
                                break;
                                

                                
                            case "%%%Closing%%%":
                            
                                
                                
                                foreach (UdpClientInfo clientim in clientInfo)
                                
                                {
                                
                                    if (clientim.IPEndPoint.Equals(clientEndPoint))
                                    {
                                        continue;
                                    }

                                    string message = $"%%%Left%%%#{client.name}|";

                                    byte[] dataio = Encoding.ASCII.GetBytes(message);

                                    EncryptSendByte(dataio, clientim);


                                };
                                
                                clientInfo.Remove(client);

                                
                                Console.WriteLine($"user {client.name} left server {Port}");

                                if(clientInfo.Count == 0)
                                {
                                    codetoport.Remove(this.meetingcode);
                                    this.Stop();
                                    UsedPorts.Remove(this.Port);
                                }
                        
                                
                                
                                break;



                        }


                            // Handle the message here (e.g., decrypt and process it)
                            
                        break;
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Log or handle other exceptions as needed
            }
        }

        public void Stop()
        {
            cts.Cancel();
            udpServer.Close();
        }
        public static (string Metadata, byte[] ImageData) GetMetadataAndData(byte[] data, string delimiter)
        {
            string receivedString = Encoding.UTF8.GetString(data);
            int delimiterIndex = receivedString.IndexOf(delimiter, StringComparison.Ordinal);

            if (delimiterIndex == -1)
            {
                throw new ArgumentException("Delimiter not found in data.");
            }

            string metadata = receivedString.Substring(0, delimiterIndex);
            byte[] imageData = new byte[data.Length - (delimiterIndex + delimiter.Length)];
            Buffer.BlockCopy(data, delimiterIndex + delimiter.Length, imageData, 0, imageData.Length);

            return (metadata, imageData);
        }
        private int FindAvailablePort(int minPort, int maxPort)
        {
            for (int i = minPort; i <= maxPort; i++)
            {
                if (!UsedPorts.Contains(i))
                {
                    UsedPorts.Add(i);
                    return i;
                }
            }
            return 0; // No available port found
        }
        private byte[] AddMetaData( byte[] message, string sender,string metatype)
        {
            
            // Original byte array
            byte[] originalBytes = message;

            // Additional data as bytes
            byte[] prefixBytes = Encoding.ASCII.GetBytes($"%%%{metatype}%%%#{sender}|");

            // Create a new array to hold the combined data
            byte[] combinedBytes = new byte[prefixBytes.Length + originalBytes.Length];

            Buffer.BlockCopy(prefixBytes, 0, combinedBytes, 0, prefixBytes.Length);

            // Copy the original bytes to the end of the combined array
            Buffer.BlockCopy(originalBytes, 0, combinedBytes, prefixBytes.Length, originalBytes.Length);

            return combinedBytes;
        }

        private void EncryptSendByte( byte[] message, UdpClientInfo sendtoclientInfo)
        {
            
            // Original byte array
            byte[] encrypted = sendtoclientInfo.aes.EncryptFrameByte(message,sendtoclientInfo.aes.Key,sendtoclientInfo.aes.IV);


            udpServer.Send(encrypted, encrypted.Length, sendtoclientInfo.IPEndPoint);
        }

        private void SendNewUser(string newusername) 
        {
            string message = $"%%%NewUser%%%#{newusername}|";
            byte[] data = Encoding.ASCII.GetBytes(message);
            foreach (UdpClientInfo client in clientInfo)
            {
                if (client.name != newusername) 
                {
                   EncryptSendByte(data, client);
                }
            }

        }
        public void InformNewUser()
        {
            string clients = "";
            foreach (UdpClientInfo client in clientInfo)
            {
                clients+= client.name;
                clients += "#";
            }
            string real = clients.Remove(clients.Length - 1);
            string message = $"%%%ExistingUsers%%%#{real}|";
            byte[] data = Encoding.ASCII.GetBytes(message);
            EncryptSendByte(data, clientInfo.Last());
        }

        public static string GenerateUniqueMeetingCode()
        {
            int length = 8;
            string code;
            do
            {
                code = GenerateRandomCode(length);
            } while (!GeneratedCodes.Add(code)); // Try until a unique code is generated

            return code;
        }
        private static string GenerateRandomCode(int length)
        {
            char[] code = new char[length];
            for (int i = 0; i < length; i++)
            {
                code[i] = AllowedCharacters[Random.Next(AllowedCharacters.Length)];
            }
            return new string(code);
        }
        private string CreateMeeting()
        {
            string code = GenerateUniqueMeetingCode();
            codetoport.Add(code,this);
            return code;

        }
    }
    

  
  
}
