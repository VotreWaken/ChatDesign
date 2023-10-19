using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Data;
namespace Server
{
    class Program
    {

        static List<User> clients;
        static void Main(string[] args)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23016);
            TcpListener server = new TcpListener(ep);
            server.Start();

            Console.WriteLine("Server started...");
            clients = new List<User>();

            while (true)
            {
                User user = new User();
                user.Client = server.AcceptTcpClient();

                BinaryFormatter bf = new BinaryFormatter();
                NetworkStream nwStream = user.Client.GetStream();

                //reading client's info
                byte[] buffer1 = new byte[255];
                int bytesRead1 = nwStream.Read(buffer1, 0, 255);
                string username = Encoding.Default.GetString(buffer1, 0, bytesRead1);

                if (CheckUsername(username))
                {
                    bf.Serialize(nwStream, new Message() { ServerMessage = ServerMessage.WrongUsername, MessageString = username });
                    continue;
                }
                user.Online = true;
                user.Username = username;
                clients.Add(user);

                //send all users back
                Console.WriteLine(user.Client.Client.RemoteEndPoint.ToString());
                bf.Serialize(nwStream, new Message() { ServerMessage = ServerMessage.UsersCollection, Users = clients });

                //send new user to everyone
                Broadcast(new Message() { ServerMessage = ServerMessage.AddUser, Sender = user });
                Task.Run(() => CatchMessages(nwStream));
            }
        }

        static void Broadcast(Message message)
        {
            foreach (var client in clients)
            {
                Task.Run(() =>
                {
                    NetworkStream nwStream = client.Client.GetStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(nwStream, message);
                });
            }
        }

        static void CatchMessages(NetworkStream stream)
        {
            BinaryFormatter bf = new BinaryFormatter();
            while (true)
            {
                try
                {
                    Message message = (Message)bf.Deserialize(stream);
                    if (message.ServerMessage == ServerMessage.Broadcast)
                        Broadcast(new Message() { MessageString = message.MessageString, Sender = message.Sender, ServerMessage = ServerMessage.Message });
                    else if (message.ServerMessage == ServerMessage.Message)
                    {
                        NetworkStream nwStream = clients.Where(x => x.Username == message.Reciever.Username).First().Client.GetStream();
                        NetworkStream nwStreamSender = clients.Where(x => x.Username == message.Sender.Username).First().Client.GetStream();
                        bf.Serialize(nwStream, new Message() { Sender = message.Sender, MessageString = message.MessageString, ServerMessage = ServerMessage.Message });
                        bf.Serialize(nwStreamSender, new Message() { Sender = message.Sender, MessageString = message.MessageString, ServerMessage = ServerMessage.Message });
                    }
                    else if (message.ServerMessage == ServerMessage.RemoveUser)
                    {
                        clients.Remove(clients.Where(x => x.Username == message.Sender.Username).First());
                        Broadcast(new Message() { ServerMessage = ServerMessage.RemoveUser, Sender = new User() { Username = message.Sender.Username } });
                        Console.WriteLine($"Disconnect User: {message.Sender}");
                    }
                }
                catch { }

            }
        }

        public static bool CheckUsername(string username)
        {
            return clients.Select(x => x.Username).Contains(username);
        }

    }
}
