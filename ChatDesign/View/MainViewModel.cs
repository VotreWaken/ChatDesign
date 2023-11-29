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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;
using ChatDesign.Model;
using System.Reflection.Metadata;

namespace ChatDesign.View
{
    class MainViewModel : Notifier
    {

        #region UI
        // Connect Visible
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

        // Warnings 
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

        // Enable Send Button 
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

        // Username Is Enable 
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

        // Textbox Message
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

        // Reciever store Username
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

        // User Profile Image
        private ImageSource userAvatar;

        public ImageSource UserAvatar
        {
            set
            {
                userAvatar = value;
                Notify();
            }
            get => userAvatar;
        }


        // Call Image
        private ImageSource callImage;
        public ImageSource CallImage
        {
            set
            {
                callImage = value;
                Notify();
            }
            get => callImage;
        }
        #endregion

        #region Commands
        public ICommand ConnectCommand { set; get; }
        public ICommand SendCommand { set; get; }
        public ICommand SendEmailCommand { set; get; }

        public ICommand SearchCommand { get; private set; }

        public ICommand ContactDoubleClickCommand { get; private set; }

        public ICommand CloseWindowCommand { get; }

        public ICommand CallCommand { get; private set; }

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
        private ObservableCollection<CustomItem> contacts;

        public ObservableCollection<CustomItem> Contacts
        {
            get { return contacts; }
        }

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
        #endregion

        public MainViewModel(string name)
        {
            Username = name;
            Bitmap bitmapavatar = GetImageFromByteArray(DbOperations.GetUserImage(Username));
            UserAvatar = ImageSourceFromBitmap(bitmapavatar);


            string imagePath = "/Assets/Call.png";


            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();

            // Set the CallImage property to the loaded image.
            CallImage = bitmapImage;

            contacts = new ObservableCollection<CustomItem>();
            foreach (var item in DbOperations.GetUserChats(DbOperations.GetUserId(Username)))
            {
                Bitmap bitmap = GetImageFromByteArray(item.Avatar);
                Contacts.Add(new CustomItem { ImagePath = ImageSourceFromBitmap(bitmap), Title = item.ChatName, IsGroupChat = item.ChatType, Id = item.ID });
            }
            SaveOriginalContacts();
            // Initialize ChatMessages
            MessagessItems = new ObservableCollection<ChatItem>();
            InitCommands();
            SendIsEnable = true;
            System.Windows.Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);
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
        //public CustomItem SelectedContact { get; set; }
        private CustomItem selectedContact;

        public CustomItem SelectedContact
        {
            get => selectedContact;
            set
            {
                if (selectedContact != value)
                {
                    // Store the current selected contact as the previous selected contact
                    previousSelectedContact = selectedContact;
                    selectedContact = value;

                    // Notify that the SelectedContact property has changed
                    Notify(nameof(SelectedContact));
                }
            }
        }
        private CustomItem previousSelectedContact;
        //public string SelectedContact
        //{
        //    set
        //    {
        //        SelectedContact = value;
        //        if (value == Users.ElementAt(0))
        //            Receiver = Users.ElementAt(0).ToLower();
        //        else
        //            Receiver = $"only {value}";

        //        Notify();
        //    }
        //    get => selectedUser;
        //}

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
                SendData(message); SaveMessageToDb();
                TextMessage = string.Empty;
            }
            ));
            SearchCommand = new RelayCommand(x => Task.Run(() =>
            {
                string searchText = SearchText;

                if (!string.IsNullOrWhiteSpace(searchText))
                {   // Add Result From Data Base To List
                    List<ChatDesign.Model.User> searchResults = DbOperations.SearchUsers(searchText);
                    MessageBox.Show(searchText + " " + searchResults.Count.ToString());
                    // Update UI
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Contacts.Clear();

                        // Get User Avatars 
                        foreach (ChatDesign.Model.User user in searchResults)
                        {
                            Bitmap bitmap = GetImageFromByteArray(user.ImagePath);
                            Contacts.Add(new CustomItem { ImagePath = ImageSourceFromBitmap(bitmap), Title = user.Name });
                        }
                    });
                }
                else
                {

                }
            }
            ));
            CallCommand = new RelayCommand(x => Task.Run(() =>
            {
                SendData(new Message { Sender = user, ServerMessage = ServerMessage.CallMessage, Reciever = user });
            }
            ));
            ContactDoubleClickCommand = new RelayCommand(x => Task.Run(() =>
            {
                //if (!Connect())
                //{
                //    UsernameTakenLabelIsEnable = Visibility.Visible;              IMPORTANT UNCOMMENT BEFORE DEBUGGING THAT 
                //    return;
                //}
                try
                {
                    if (SelectedContact is CustomItem selectedContact)
                    {
                        // Check if the current chat is different from the previous chat
                        if (previousSelectedContact != null && selectedContact.Id != previousSelectedContact.Id)
                        {

                            if (selectedContact.Id != previousSelectedContact.Id)
                            {
                                SendData(new Message { Sender = user, ServerMessage = ServerMessage.RemoveUser });
                            }
                        }
                        else if (previousSelectedContact != null && selectedContact.Id == previousSelectedContact.Id)
                        {
                            return;
                        }
                        int chatID = selectedContact.Id;

                        // Load chat messages for the selected contact
                        ObservableCollection<ChatItem> chatMessages = DbOperations.LoadChatMessagesFromDatabase(chatID);
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Clear and update the ChatMessages collection
                            MessagessItems.Clear();
                            foreach (var message in chatMessages)
                            {
                                MessagessItems.Add(message);
                            }
                        });

                        ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23016);
                        user = new Data.User();

                        Users = new ObservableCollection<string>();
                        Users.Add("Everyone");
                        SelectedUser = Users.ElementAt(0);
                        ConnectIsVisible = Visibility.Visible;
                        WarningVisibility = UsernameTakenLabelIsEnable = Visibility.Hidden;

                        if (ConnectCommand.CanExecute(selectedContact))
                        {
                            ConnectCommand.Execute(selectedContact);
                        }

                        // Update previousSelectedContact to the current selectedContact
                        previousSelectedContact = selectedContact;
                    }
                }
                catch (Exception ex)
                {
                    return;
                    //MessageBox.Show(ex.Message);
                }
            }));
        }



        public void SaveMessageToDb()
        {
            MessagesDataBase message = new MessagesDataBase
            {
                SenderId = DbOperations.GetUserId(Username),
                ReceiverId = selectedContact.Id,
                Content = TextMessage,
            };

            // Call the method to add the message to the database
            DbOperations.AddMessage(message);

        }

        public bool Connect()
        {
            user.Client = new TcpClient();
            user.Client.Connect(ep);
            nwStream = user.Client.GetStream();

            Username = Username ?? "Unknown";


            string combinedInfo = Username + "||" + SelectedContact.Title;

            byte[] data = Encoding.Default.GetBytes(combinedInfo);

            nwStream.Write(data, 0, data.Length);
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

        // Get Data From Server
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
                                MessagessItems.Add(new ChatItem() { Sender = $"{Username}: ", Content = message.MessageString, IsSender = true });
                            else
                                MessagessItems.Add(new ChatItem() { Sender = $"{message.Sender.Username}: ", Content = message.MessageString, IsSender = false });
                        }));
                    }
                    else if (message.ServerMessage == ServerMessage.AddUser)
                    {


                        App.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            MessagessItems.Add(new ChatItem() { Sender = message.Sender.Username, Content = " Joined the chat", IsSender = true });
                            if (!Users.Contains(message.Sender.Username))
                                Users.Add($"{message.Sender.Username}");
                        }));
                    }
                    else if (message.ServerMessage == ServerMessage.RemoveUser)
                    {

                        App.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            MessagessItems.Add(new ChatItem() { Sender = message.Sender.Username, Content = " Has left the chat", IsSender = true });
                            Users.Remove(Users.Where(x => x == message.Sender.Username).First());
                        }));
                    }
                    else if (message.ServerMessage == ServerMessage.CallMessage)
                    {
                        App.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            MessagessItems.Add(new ChatItem() { Sender = message.Sender.Username, Content = " Started Audio Call", IsSender = true });
                            string imagePath = "/Assets/GreenCall.png";
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                            bitmapImage.EndInit();
                            if (message.MessageString == "0")
                            {
                                try
                                {
                                    App.Current.Dispatcher.Invoke(() =>
                                    {
                                        CallWindowServer CallWindow = new CallWindowServer(username,UserAvatar);
                                        CallWindow.Show();
                                    });
                                }
                                catch (Exception)
                                {

                                }
                            }
                            else
                            {
                                try
                                {
                                    App.Current.Dispatcher.Invoke(() =>
                                    {
                                        CallWindow callWindowClient = new();
                                        callWindowClient.Show();
                                    });
                                }
                                catch (Exception)
                                {

                                }
                            }
                            // Set the CallImage property to the loaded image.
                            CallImage = bitmapImage;
                        }));
                    }
                }
                catch (Exception e)
                {
                    return; //MessageBox.Show(e.Message); 
                }
            }
        }

        // Send Data To Server
        public void SendData(Message message)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(nwStream, message);
        }


        private List<CustomItem> originalItems = new List<CustomItem>();
        private void SaveOriginalContacts()
        {
            for (int i = 0; i < Contacts.Count; i++)
            {
                originalItems.Add((CustomItem)Contacts[i]);
            }
        }
        public string ContactsSearch
        {
            set
            {
                ContactsSearch = value;
                Notify();
            }
            get => ContactsSearch;
        }
        private string searchText;
        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (searchText != value)
                {
                    searchText = value;
                    Notify();
                }
            }
        }
        private void FilterContacts()
        {
            string searchText = SearchText;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // If the search text is empty, no filtering is needed.
                // You can update the Contacts collection to the original items.
                Contacts.Clear();
                foreach (CustomItem item in originalItems)
                {
                    Contacts.Add(item);
                }
            }
            else
            {
                // Create a filtered list to store the items that match the search text.
                List<CustomItem> filteredItems = new List<CustomItem>();

                foreach (CustomItem item in originalItems)
                {
                    if (item.Title != null && item.Title.Contains(searchText))
                    {
                        filteredItems.Add(item);
                    }
                }

                // Update the Contacts collection with the filtered items.
                Contacts.Clear();
                foreach (CustomItem item in filteredItems)
                {
                    Contacts.Add(item);
                }
            }
        }
        // Image Handler
        #region Image Handler 
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {

            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
        public Byte[] ReturnImagesByteArray()
        {
            return DbOperations.GetImages();
        }

        private static readonly ImageConverter _imageConverter = new ImageConverter();
        public static Bitmap GetImageFromByteArray(byte[] byteArray)
        {

            Bitmap bm = (Bitmap)_imageConverter.ConvertFrom(byteArray);

            if (bm != null && (bm.HorizontalResolution != (int)bm.HorizontalResolution ||
                               bm.VerticalResolution != (int)bm.VerticalResolution))
            {
                // Correct a strange glitch that has been observed in the test program when converting 
                //  from a PNG file image created by CopyImageToByteArray() - the dpi value "drifts" 
                //  slightly away from the nominal integer value
                bm.SetResolution((int)(bm.HorizontalResolution + 0.5f),
                                 (int)(bm.VerticalResolution + 0.5f));
            }

            return bm;
        }
        #endregion

        // Window Closing Event 
        public void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                SendData(new Message { Sender = user, ServerMessage = ServerMessage.RemoveUser });
            }
            catch { }
        }


        // button 

        public ICommand AddCommand => new BtnCommand(() =>
        {
            MessageBox.Show("Add command executed");
        });

        public ICommand MoveCommand => new BtnCommand(() =>
        {
            MessageBox.Show("Move command executed");
        });

        public ICommand DeleteCommand => new BtnCommand(() =>
        {
            MessageBox.Show("Delete command executed");
        });

    }
}
