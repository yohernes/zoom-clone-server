using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Net;

namespace server
{/// <summary>
 /// The ChatClient class represents info about each client connecting to the server.
 /// </summary>
    class Client
    {
        // Store list of all clients connecting to the server
        // the list is static so all memebers of the chat will be able to obtain list
        // of current connected client
        public static Hashtable AllClients = new Hashtable();
     

        // information about the client
        private TcpClient _client;
        private IPEndPoint ipend;
        public bool inMeeting;
        public UdpServer inServer;

        private string _clientIP;

        // used for sending and reciving data
        private byte[] data;

        // the nickname being sent
        private RSA RSA;
        private string ClientPublicKey;
        private string ServerPublicKey;
        private AesEncryption aes;
        string connectionString;

        /// <summary>
        /// When the client gets connected to the server the server will create an instance of the ChatClient and pass the TcpClient
        /// </summary>
        /// <param name="client"></param>
        public Client(TcpClient client)
        {
            RSA = new RSA();
            _client = client;
            ipend = _client.Client.RemoteEndPoint as IPEndPoint;
            connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Users\studentc\Downloads\final project\server\server\User.mdf"";Integrated Security=True";
            aes = new AesEncryption();

            // get the ip address of the client to register him with our client list
            _clientIP = client.Client.RemoteEndPoint.ToString();

            // Add the new client to our clients collection
            AllClients.Add(_clientIP, this);
            // Read data from the client async
            data = new byte[_client.ReceiveBufferSize];
            // BeginRead will begin async read from the NetworkStream
            // This allows the server to remain responsive and continue accepting new connections from other clients
            // When reading complete control will be transfered to the ReviveMessage() function.
            _client.GetStream().BeginRead(data,
                                          0,
                                          System.Convert.ToInt32(_client.ReceiveBufferSize),
                                          ReceiveMessage,
                                          null);


        }
        public void ReceiveMessage(IAsyncResult ar)
        {
            int bytesRead;
            try
            {
                lock (_client.GetStream())
                {
                    // call EndRead to handle the end of an async read.
                    bytesRead = _client.GetStream().EndRead(ar);
                }
                
                if (bytesRead < 1)
                {
                    // remove the client from out list of clients
                    AllClients.Remove(_clientIP);

                    return;
                }
                // client still connected
                {
                    
                    string messageReceived = System.Text.Encoding.ASCII.GetString(data, 0, bytesRead);
                    Console.WriteLine(messageReceived);

                    if (messageReceived.StartsWith("%%%clientPublicKey%%%")|| messageReceived.StartsWith("%%%ClientAesKey%%%"))
                    {
                        DoHandshake(messageReceived);
                    }
                    
                    //after handshake
                    else
                    {
                        messageReceived = aes.Decrypt(messageReceived);
                        Console.WriteLine(messageReceived);
                    }
   
                    if (messageReceived.StartsWith("%%%regist%%%")|| (messageReceived.StartsWith("%%%login%%%")|| 
                             messageReceived.StartsWith("%%%NewMeeting%%%")|| messageReceived.StartsWith("%%%JoinMeeting%%%")) || messageReceived.StartsWith("%%%Chat%%%"))
                    {
                        DoClientStuff(messageReceived);
                    }
                    
                    
                    
                    
                }
                lock (_client.GetStream())
                {
                    // continue reading form the client
                    _client.GetStream().BeginRead(data, 0, System.Convert.ToInt32(_client.ReceiveBufferSize), ReceiveMessage, null);
                    //SendMessage(System.Text.Encoding.ASCII.GetString(data, 0, bytesRead) + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                AllClients.Remove(_clientIP);
                //Broadcast(_ClientNick + " has left the chat.");
            }
        }
        //static string ToHash(string input)
        //{
        //    using (SHA256 sha256 = SHA256.Create())
        //    {
        //        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        //        byte[] hashBytes = sha256.ComputeHash(inputBytes);

        //        StringBuilder builder = new StringBuilder();
        //        for (int i = 0; i < hashBytes.Length; i++)
        //        {
        //            builder.Append(hashBytes[i].ToString("x2")); // Convert to hexadecimal
        //        }

        //        return builder.ToString();
        //    }
        //}

        private void DoHandshake(string messageReceived)
        {
            

            if (messageReceived.StartsWith("%%%clientPublicKey%%%"))
            {
                ClientPublicKey = messageReceived.Substring(21);
                Console.WriteLine(ClientPublicKey);
                Console.Write(RSA.GetPublicKey());
                ServerPublicKey = RSA.GetPublicKey();
                SendMessage("%%%ServerPublicKey%%%" + ServerPublicKey);
                return;
            }


            if (messageReceived.StartsWith("%%%ClientAesKey%%%"))
            {
                try
                {
                    string keyandIV = messageReceived.Substring(18);
                    keyandIV = RSA.Decrypt(keyandIV);

                    string[] arrkeyandIV = keyandIV.Split('|');
                    if (arrkeyandIV.Length >= 2)
                    {
                        string unkey = arrkeyandIV[0];
                        string unIV = arrkeyandIV[1];

                        byte[] aesKey = Convert.FromBase64String(unkey);
                        byte[] IV = Convert.FromBase64String(unIV);

                        // Check if the decrypted key and IV are valid
                        if (aesKey.Length == 16 && IV.Length == 16) // Assuming AES-128
                        {
                            aes.Key = aesKey;
                            aes.IV = IV;
                           
                            SendMessage("%%%Handshake%%%");
                        }
                        else
                        {
                            SendMessage("Invalid AES key or IV length.");
                        }
                    }
                    else
                    {
                        SendMessage("Invalid AES key and IV format.");
                    }
                }
                catch (Exception ex)
                {
                    SendMessage("Error decrypting AES key and IV: " + ex.Message);
                }
                return;
            }

        }


        private void DoClientStuff(string messageReceived)
        {
           

            if (messageReceived.StartsWith("%%%regist%%%"))
            {
                //if regist ok
                string[] userdetails = messageReceived.Substring(13).Split('#');
                //userdetails[1] = ToHash(userdetails[1]);
                //Console.WriteLine(userdetails[1]);
                if (!IsExist(userdetails[0]))
                {
                    int x = InsertUser(userdetails);
                    if (x > 0)
                        SendMessage("registOK");
                }
                else
                    SendMessage("registNotOK");

                //if regist not ok
                //SendMessage("regist NOT OK");
            }
            else if (messageReceived.StartsWith("%%%login%%%"))
            {
                string[] nameApassword = messageReceived.Substring(12).Split('#');

                //nameApassword[1] = ToHash(nameApassword[1]);
                //if login ok;
                if (IsExist(nameApassword))
                {
                    SendMessage($"%%%LoginOK%%%{nameApassword[0]}");

                }
                    
                else
                    SendMessage("LoginNOTOK");
            }
            else if (messageReceived.StartsWith("%%%NewMeeting%%%"))
            {

                string[] message= messageReceived.Substring(16).Split('#');
                string user = message[0];
                IPAddress userip = ipend.Address;
                IPEndPoint udpendpoint = new IPEndPoint(userip, int.Parse(message[1]));

                UdpServer udpServer = new UdpServer();
                Thread serverThread = new Thread(new ThreadStart(udpServer.Start));
                serverThread.Start();
                Thread.Sleep(500);

                udpServer.AddClient(aes, user, udpendpoint);
                //ServerList.Add(udpServer);
                SendMessage($"%%%NewMeeting%%%{udpServer.Port}#{user}#{udpServer.meetingcode}");
                inMeeting = true;
                inServer = udpServer;
            }
            else if (messageReceived.StartsWith("%%%JoinMeeting%%%"))
            {
                string[] data = messageReceived.Substring(17).Split('#');
                string code =data[0];
                string user = data[1];
                IPAddress userip = ipend.Address;
                IPEndPoint udpendpoint = new IPEndPoint(userip, int.Parse(data[2]));
                UdpServer current = null;
                
                if (UdpServer.codetoport.ContainsKey(code))
                {
                    foreach(var server in UdpServer.codetoport)
                    {
                        if (server.Key == code)
                        {
                            current = server.Value;
                        }
                        
                    }

                    try
                    {
                        current.AddClient(aes, user, udpendpoint);



                    }
                    catch
                    {
                        SendMessage($"%%%ServerIssue%%%404");
                    }
                    SendMessage($"%%%JoinPort%%%{current.Port}#{user}#{code}");
                    Thread.Sleep(500);
                    current.InformNewUser();
                    inMeeting = true;
                    inServer = current;
                }
                else
                {
                    SendMessage("WrongCode");
                }

               
                
            }
            else if (messageReceived.StartsWith("%%%Chat%%%"))
            {

                string[] message = messageReceived.Substring(10).Split('#');
                string user = message[0];
                string chatmessage = message[1];
                Console.WriteLine($"received chat message from {user}");
                foreach (DictionaryEntry client in AllClients)
                {
                    if (((Client)(client.Value)).inMeeting)
                    {
                        if (((Client)(client.Value)).inServer == this.inServer)
                        {
                            ((Client)(client.Value)).SendMessage($"%%%Chat%%%{user}#{chatmessage}");
                        }
                    }
                }
            }
        }
        public void SendMessage(string message)
        {
            try
            {
                NetworkStream ns;

                // we use lock to present multiple threads from using the networkstream object
                // this is likely to occur when the server is connected to multiple clients all of 
                // them trying to access to the networkstram at the same time.
                lock (_client.GetStream())
                {
                    ns = _client.GetStream();
                }

                // Send data to the client
                if (!message.StartsWith("%%%ServerPublicKey%%%"))
                {
                    message = aes.Encrypt(message);
                }
                byte[] bytesToSend = System.Text.Encoding.ASCII.GetBytes(message);
                ns.Write(bytesToSend, 0, bytesToSend.Length);
                ns.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private int InsertUser(string[] userdetails)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand();
                SqlTransaction transaction = null;

                try
                {
                    // Hash the password
                    userdetails[1] = ToHash(userdetails[1]);

                    // Log the lengths of each parameter
                    Console.WriteLine("Username length: " + userdetails[0].Length);
                    Console.WriteLine("Password length: " + userdetails[1].Length);
                    Console.WriteLine("Firstname length: " + userdetails[2].Length);
                    Console.WriteLine("Lastname length: " + userdetails[3].Length);
                    Console.WriteLine("Email length: " + userdetails[4].Length);
                    Console.WriteLine("Region length: " + userdetails[5].Length);

                    // Ensure data does not exceed column lengths
                    if (userdetails[0].Length > 50 ||
                        userdetails[1].Length > 64 ||
                        userdetails[2].Length > 50 ||
                        userdetails[3].Length > 50 ||
                        userdetails[4].Length > 50 ||
                        userdetails[5].Length > 50)
                    {
                        throw new Exception("One or more input values exceed the defined column length.");
                    }

                    // Prepare the command and the SQL query
                    cmd.Connection = connection;
                    string sql = "INSERT INTO userDetail (username, password, firstname, lastname, email, region) " +
                                 "VALUES (@Username, @Password, @Firstname, @Lastname, @Email, @Region)";
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@Username", userdetails[0].Trim());
                    cmd.Parameters.AddWithValue("@Password", userdetails[1].Trim());
                    cmd.Parameters.AddWithValue("@Firstname", userdetails[2].Trim());
                    cmd.Parameters.AddWithValue("@Lastname", userdetails[3].Trim());
                    cmd.Parameters.AddWithValue("@Email", userdetails[4].Trim());
                    cmd.Parameters.AddWithValue("@Region", userdetails[5].Trim());

                    connection.Open();
                    transaction = connection.BeginTransaction();
                    cmd.Transaction = transaction;

                    int rowsAffected = cmd.ExecuteNonQuery();
                    transaction.Commit();

                    return rowsAffected;
                }
                catch (Exception ex)
                {
                    // Rollback the transaction on error
                    if (transaction != null)
                    {
                        transaction.Rollback();
                    }

                    // Log the error message
                    Console.WriteLine(ex.Message);
                    return 0;
                }
                finally
                {
                    // Ensure the connection is closed
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
        }

        private string ToHash(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }




        private bool IsExist(string userdetails)
        {
            // Ensure the connection object is properly disposed of
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Define the query with parameters
                string sql = "SELECT COUNT(username) FROM userDetail WHERE username=@Username";

                // Create the command object and associate it with the connection
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@Username", userdetails);

                    // Open the connection
                    connection.Open();

                    // Execute the query and get the result
                    int count = (int)cmd.ExecuteScalar();

                    // Return true if the user exists, otherwise false
                    return count > 0;
                }
            }
        }

        private bool IsExist(string[] userdetails)
        {
            userdetails[1] = ToHash(userdetails[1]);
            // Ensure the connection object is properly disposed of
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Define the query with parameters
                string sql = "SELECT COUNT(username) FROM userDetail WHERE username=@username AND password=@password";

                // Create the command object
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@username", userdetails[0]);
                    cmd.Parameters.AddWithValue("@password", userdetails[1]);

                    // Open the connection
                    connection.Open();

                    // Execute the query and get the result
                    int count = (int)cmd.ExecuteScalar();

                    // Return true if the user exists, otherwise false
                    return count > 0;
                }
            }
        }

       
    }
}
