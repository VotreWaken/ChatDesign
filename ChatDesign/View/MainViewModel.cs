using ChatDesign.Control;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ChatDesign.Model;
using System.Collections.ObjectModel;
using Data;
using System.Data;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Controls;

namespace ChatDesign.View
{
    class MainViewModel : Notifier
    {

        #region UI
        private Visibility connectIsVisible;
        public Visibility ConnectIsVisible
        {
            set
            {
                connectIsVisible = value;
                Notify();
            }
            get => connectIsVisible;
        }

        private Visibility warningVisibility;
        public Visibility WarningVisibility
        {
            set
            {
                warningVisibility = value;
                Notify();
            }
            get => warningVisibility;
        }

        private bool sendIsEnable;
        public bool SendIsEnable
        {
            set
            {
                sendIsEnable = value;
                Notify();
            }
            get => sendIsEnable;
        }

        private Visibility usernameTakenLabelIsEnable;
        public Visibility UsernameTakenLabelIsEnable
        {
            set
            {
                usernameTakenLabelIsEnable = value;
                Notify();
            }
            get => usernameTakenLabelIsEnable;
        }

        private string textMessage;
        public string TextMessage
        {
            set
            {
                textMessage = value;
                Notify();
            }
            get => textMessage;
        }

        private string receiver;
        public string Receiver
        {
            set
            {
                receiver = value;
                Notify();
            }
            get => receiver;
        }



        #endregion

        #region Commands
        public ICommand ConnectCommand { set; get; }
        public ICommand SendCommand { set; get; }
        public ICommand SendEmailCommand { set; get; }
        #endregion

        #region Fields
        private Data.User user;
        private IPEndPoint ep;
        private NetworkStream nwStream;

        private string selectedUser;
        private string username;
        #endregion

        #region Properties
        public ObservableCollection<ChatItem> MessagessItems { set; get; }

        public string SelectedUser
        {
            set
            {
                selectedUser = value;
                if (value == Users.ElementAt(0))
                    Receiver = Users.ElementAt(0).ToLower();
                else
                    Receiver = $"only {value}";

                Notify();
            }
            get => selectedUser;
        }

        public string Username
        {
            set
            {
                username = value;
                Notify();
            }
            get => username;
        }

        public ObservableCollection<string> Users { set; get; }
        public string EmailAddress { set; get; }
        public string Online
        {
            set
            {
                Online = value;
                Notify();
            }
            get => username;
        }
        #endregion

        public MainViewModel()
        {
            ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23016);
            user = new Data.User();

            Users = new ObservableCollection<string>();
            Users.Add("Everyone");
            SelectedUser = Users.ElementAt(0);

            ConnectIsVisible = Visibility.Visible;
            WarningVisibility = UsernameTakenLabelIsEnable = Visibility.Hidden;
            MessagessItems = new ObservableCollection<ChatItem>();
            InitCommands();
            Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);
        }

        public void LoadMessages(int chatID)
        {
            // Load chat messages for the selected contact
            ObservableCollection<ChatItem> chatMessages = DbOperations.LoadChatMessagesFromDatabase(chatID);

            for (int i = 0; i < chatMessages.Count; i++)
            {

                MessagessItems.Add(chatMessages[i]);
            }
        }

        public MainViewModel(string name)
        {
            ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23016);
            user = new Data.User();
            
            Users = new ObservableCollection<string>();
            Users.Add("Everyone");
            SelectedUser = Users.ElementAt(0);
            Username = name;
            ConnectIsVisible = Visibility.Visible;
            WarningVisibility = UsernameTakenLabelIsEnable = Visibility.Hidden;
            MessagessItems = new ObservableCollection<ChatItem>();
            InitCommands();

            Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);
        }

        private void InitCommands()
        {
            ConnectCommand = new RelayCommand(x => Task.Run(() =>
            {
                if (!Connect())
                {
                    UsernameTakenLabelIsEnable = Visibility.Visible;
                    return;
                }

                ConnectIsVisible = UsernameTakenLabelIsEnable = Visibility.Hidden;
                WarningVisibility = Visibility.Visible;
                SendIsEnable = true;
                user.Username = Username;

                GetData();
            }));

            SendCommand = new RelayCommand(x => Task.Run(() =>
            {
                Message message = new Message()
                {
                    MessageString = TextMessage,
                    Sender = user
                };

                if (Users.ElementAt(0) == SelectedUser || SelectedUser == null)
                    message.ServerMessage = ServerMessage.Broadcast;
                else
                {
                    message.ServerMessage = ServerMessage.Message;
                    message.Reciever = new Data.User() { Username = SelectedUser };
                }
                SendData(message);
                TextMessage = string.Empty;
            }
            ));
        }


        public bool Connect()
        {
            user.Client = new TcpClient();
            user.Client.Connect(ep);
            nwStream = user.Client.GetStream();

            Username = Username ?? "Unknown";
            nwStream.Write(Encoding.Default.GetBytes(Username), 0, Username.Length);
            nwStream.Flush();

            BinaryFormatter bf = new BinaryFormatter();
            Message message = (Message)bf.Deserialize(nwStream);
            if (message.ServerMessage == ServerMessage.WrongUsername)
            {
                user.Client.Client.Shutdown(SocketShutdown.Both);
                user.Client.Close();
                return false;
            }
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                foreach (var i in message.Users)
                    Users.Add(i.Username);
            }));

            return true;
        }

        public void GetData()
        {
            BinaryFormatter bf = new BinaryFormatter();
            while (true)
            {
                
                try
                {
                    Message message = (Message)bf.Deserialize(nwStream);
                    if (message.ServerMessage == ServerMessage.Message)
                    {

                        App.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            if (message.Sender.Username == Username)
                                MessagessItems.Add(new ChatItem() { Sender = $"me: ", Content = message.MessageString, IsSender = false });
                            else
                                MessagessItems.Add(new ChatItem() { Sender = $"{message.Sender.Username}: ", Content = message.MessageString, IsSender = false });
                        }));
                    }
                    else if (message.ServerMessage == ServerMessage.AddUser)
                    {


                        App.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            MessagessItems.Add(new ChatItem() { Sender = message.Sender.Username, Content = " joined the chat", IsSender = true });
                            if (!Users.Contains(message.Sender.Username))
                                Users.Add($"{message.Sender.Username}");
                        }));
                    }
                    else if (message.ServerMessage == ServerMessage.RemoveUser)
                    {

                        App.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            MessagessItems.Add(new ChatItem() { Sender = message.Sender.Username, Content = " has left the chat", IsSender = true });
                            Users.Remove(Users.Where(x => x == message.Sender.Username).First());
                        }));
                    }
                }

                catch (Exception e) { MessageBox.Show(e.Message); }

            }
        }

        public void SendData(Message message)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(nwStream, message);
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                SendData(new Message { Sender = user, ServerMessage = ServerMessage.RemoveUser });
            }
            catch { }
        }

    }
}
