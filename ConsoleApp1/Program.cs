using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Data;
using System.Net.Mail;

namespace Server
{
    class Program
    {
        static List<User> clients;
        static Dictionary<string, List<User>> chatRooms; // Store chat rooms

        static void Main(string[] args)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23016);
            TcpListener server = new TcpListener(ep);
            server.Start();

            Console.WriteLine("Server started...");
            clients = new List<User>();
            chatRooms = new Dictionary<string, List<User>>(); // Initialize chat rooms

            while (true)
            {
                User user = new User();
                user.Client = server.AcceptTcpClient();
                BinaryFormatter bf = new BinaryFormatter();
                NetworkStream nwStream = user.Client.GetStream();

                // Reading client's info
                byte[] buffer1 = new byte[255];
                int bytesRead1 = nwStream.Read(buffer1, 0, 255);
                string combinedInfo = Encoding.Default.GetString(buffer1, 0, bytesRead1);

                string[] infoParts = combinedInfo.Split(new string[] { "||" }, StringSplitOptions.None);

                string username = infoParts[0];
                string chatRoomName = infoParts[1];
                Console.WriteLine(username + " Joined " + chatRoomName);
                if (CheckUsername(username))
                {
                    bf.Serialize(nwStream, new Message() { ServerMessage = ServerMessage.WrongUsername, MessageString = username });
                    continue;
                }

                user.Online = true;
                user.Username = username;
                clients.Add(user);


                JoinChatRoom(user, chatRoomName);

                // Send all users back
                bf.Serialize(nwStream, new Message() { ServerMessage = ServerMessage.UsersCollection, Users = clients });

                // Send new user to everyone in the same chat room
                Broadcast(new Message() { ServerMessage = ServerMessage.AddUser, Sender = user }, chatRoomName);
                Task.Run(() => CatchMessages(nwStream, user, chatRoomName));
            }
        }

        static void Broadcast(Message message, string chatRoomName)
        {
            foreach (var client in chatRooms[chatRoomName])
            {
                Task.Run(() =>
                {
                    NetworkStream nwStream = client.Client.GetStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(nwStream, message);
                });
            }
        }
        static public bool AudioCallServer { get; set; }
        static void CatchMessages(NetworkStream stream, User user, string chatRoomName)
        {
            BinaryFormatter bf = new BinaryFormatter();
            while (true)
            {
                try
                {
                    Message message = (Message)bf.Deserialize(stream);
                    if (message.ServerMessage == ServerMessage.Broadcast)
                        Broadcast(new Message() { MessageString = message.MessageString, Sender = user, ServerMessage = ServerMessage.Message }, chatRoomName);
                    else if (message.ServerMessage == ServerMessage.Message)
                    {
                        User receiver = GetUserByName(message.Reciever.Username, chatRoomName);
                        if (receiver != null)
                        {
                            SendMessageToUser(user, receiver, message, chatRoomName);
                        }
                    }
                    else if (message.ServerMessage == ServerMessage.RemoveUser)
                    {
                        clients.Remove(user);
                        chatRooms[chatRoomName].Remove(user);
                        Broadcast(new Message() { ServerMessage = ServerMessage.RemoveUser, Sender = user }, chatRoomName);
                        Console.WriteLine($"User disconnected: {user.Username} From {chatRoomName}");
                    }
                    else if (message.ServerMessage == ServerMessage.CallMessage)
                    {
                        if (AudioCallServer == false)
                        {
                            User receiver = GetUserByName(message.Reciever.Username, chatRoomName);
                            Console.WriteLine("Server Started" + message.Reciever.Username);

                            // Отправить сообщение о звонке с информацией о пользователях
                            SendMessageToUser(user, receiver, new CallMessage(
                                user,
                                "0", // Здесь устанавливаем "0" в MessageString для сигнала начала звонка
                                chatRooms[chatRoomName].Select(p => new UserInfo { ImageBytes = p.GetUserImageBytes(), Username = p.Username }).ToList()
                            ), chatRoomName);

                            Console.WriteLine("Name: " + receiver.Username);
                            Console.WriteLine("Image: " + receiver.ImageBytes);
                            Console.WriteLine($"Joined to Audio Call: {user.Username}");
                            AudioCallServer = true;
                        }
                        else
                        {
                            Console.WriteLine("Client Started" + message.Reciever.Username);
                            User receiver = GetUserByName(message.Reciever.Username, chatRoomName);

                            // Отправить сообщение о звонке с сигналом начала аудиозвонка
                            SendMessageToUser(user, receiver, new Message() { ServerMessage = ServerMessage.CallMessage, Sender = user, MessageString = "0" }, chatRoomName);
                            Console.WriteLine($"Started Audio Call: {user.Username}");
                        }
                    }
                }

                catch { }
            }
        }
        static void JoinChatRoom(User user, string chatRoomName)
        {
            if (!chatRooms.ContainsKey(chatRoomName))
                chatRooms[chatRoomName] = new List<User>();

            chatRooms[chatRoomName].Add(user);

        }

        public static bool CheckUsername(string username)
        {
            return clients.Select(x => x.Username).Contains(username);
        }

        static User GetUserByName(string username, string chatRoomName)
        {
            return chatRooms.ContainsKey(chatRoomName)
                ? chatRooms[chatRoomName].FirstOrDefault(user => user.Username == username)
                : null;
        }

        static void SendMessageToUser(User sender, User receiver, Message message, string chatRoomName)
        {
            NetworkStream nwStream = receiver.Client.GetStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(nwStream, message);
        }
    }
}
