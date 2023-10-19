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

        public ICommand SearchCommand { get; private set; }

        public ICommand ContactDoubleClickCommand { get; private set; }

        public ICommand CloseWindowCommand { get; }
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

        public MainViewModel(string name)
        {
            Username = name;
            Bitmap bitmapavatar = GetImageFromByteArray(DbOperations.GetUserImage(Username));
            UserAvatar = ImageSourceFromBitmap(bitmapavatar);
            contacts = new ObservableCollection<CustomItem>();
            foreach (var item in DbOperations.GetUserChats(DbOperations.GetUserId(Username)))
            {

                Bitmap bitmap = GetImageFromByteArray(item.Avatar);
                Contacts.Add(new CustomItem { ImagePath = ImageSourceFromBitmap(bitmap), Title = item.ChatName, Id = item.ID });

            }
            SaveOriginalContacts();
            // Initialize ChatMessages
            MessagessItems = new ObservableCollection<ChatItem>();
            InitCommands();
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

        //public MainViewModel(string name)
        //{
        //    ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23016);
        //    user = new Data.User();

        //    Users = new ObservableCollection<string>();
        //    Users.Add("Everyone");
        //    SelectedUser = Users.ElementAt(0);
        //    Username = name;
        //    ConnectIsVisible = Visibility.Visible;
        //    WarningVisibility = UsernameTakenLabelIsEnable = Visibility.Hidden;
        //    MessagessItems = new ObservableCollection<ChatItem>();
        //    InitCommands();

        //    System.Windows.Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);
        //}
        public CustomItem SelectedContact { get; set; }

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
            SearchCommand = new RelayCommand(x => Task.Run(() =>
            {
                string searchText = ContactsSearch;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    List<ChatDesign.Model.User> searchResults = DbOperations.SearchUsers(searchText);
                    MessageBox.Show(searchResults.Count.ToString());
                    // Update your UI (e.g., a ListView or ListBox) with the search results.
                    Contacts.Clear();

                    foreach (ChatDesign.Model.User user in searchResults)
                    {
                        Bitmap bitmap = GetImageFromByteArray(user.ImagePath);
                        Contacts.Add(new CustomItem { ImagePath = ImageSourceFromBitmap(bitmap), Title = user.Name });
                    }
                }
                else
                {
                    // Handle the case when the search text is empty (e.g., show all users).
                    // You can reload the original list of users or display a message.
                }
            }
            ));
            ContactDoubleClickCommand = new RelayCommand(x => Task.Run(() =>
            {
                if (SelectedContact is CustomItem selectedContact)
                {
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
                }
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
                                MessagessItems.Add(new ChatItem() { Sender = $"{Username}: ", Content = message.MessageString, IsSender = true });
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

        public void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                SendData(new Message { Sender = user, ServerMessage = ServerMessage.RemoveUser });
            }
            catch { }
        }



        private ImageSource userAvatar;

        public ImageSource UserAvatar
        {
            set
            {
                userAvatar = value;
                Notify(); // Notify property change here
            }
            get => userAvatar;
        }

        private void MyScrollViewer_ViewChanged(object sender, Control.MyScrollViewer e)
        {
            var scrollViewer = (ScrollViewer)sender;

            // You can determine when to load more data based on your requirements
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
            {
                // Load more data here
                //YourIncrementalLoadingCollectionInstance.LoadMoreItemsAsync(20); // Load 20 more items as an example
            }
        }
        public MainViewModel(string username, byte[] image)
        {
            Bitmap bitmapavatar = GetImageFromByteArray(image);
            UserAvatar = ImageSourceFromBitmap(bitmapavatar);

            foreach (var item in DbOperations.GetUserChats(DbOperations.GetUserId(username)))
            {
                Bitmap bitmap = GetImageFromByteArray(item.Avatar);
                Contacts.Add(new CustomItem { ImagePath = ImageSourceFromBitmap(bitmap), Title = item.ChatName, Id = item.ID });
                
            }
            SaveOriginalContacts();
            // Initialize ChatMessages
            MessagessItems = new ObservableCollection<ChatItem>();
            System.Windows.Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);
        }
        private void SendMessage(object sender, RoutedEventArgs e)
        {
            //MainViewModel viewModel = (MainViewModel)DataContext;
            //MainViewModel viewModel = (MainViewModel)DataContext;
            //ChatItem i = new ChatItem { Content = TextMessage, IsSender = true, Sender = Username };
            ////ChatListBox.Items.Add(i);
            //ChatMessages.Add(i);
        }
        //private void ContactItem_DoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    // Get the selected item
        //    CustomItem selectedItem = (CustomItem)Contacts.SelectedItem;

        //    // Check if an item is selected
        //    if (selectedItem != null)
        //    {
        //        LoadMessages(((CustomItem)Contacts.SelectedItem).Id);
        //        // Invoke the ConnectCommand with the selected item as a parameter
        //        if (ConnectCommand.CanExecute(selectedItem))
        //        {
        //            ConnectCommand.Execute(selectedItem);
        //        }
        //    }
        //    //DataContext = this;
        //    int chatID = ((CustomItem)Contacts.SelectedItem).Id;

        //    // Load chat messages for the selected contact
        //    ObservableCollection<ChatItem> chatMessages = DbOperations.LoadChatMessagesFromDatabase(chatID);

        //    // Clear and update the ChatListBox
        //    ChatMessages.Clear();
        //    foreach (var message in chatMessages)
        //    {
        //        ChatMessages.Add(message);
        //    }
        //}

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

        public ObservableCollection<ChatItem> ChatMessages { get; set; } = new ObservableCollection<ChatItem>();

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
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = ContactsSearch; // Replace YourTextBox with your TextBox's name

            if (string.IsNullOrWhiteSpace(searchText))
            {
                Contacts.Clear();
                // If the search text is empty, no filtering is needed, so you can leave the original items as they are.
                for (int i = 0; i < originalItems.Count; i++)
                {
                    Contacts.Add((CustomItem)originalItems[i]);
                }
            }
            else
            {
                // Create a filtered list to store the items that match the search text.
                List<CustomItem> filteredItems = new List<CustomItem>();

                foreach (CustomItem item in Contacts)
                {
                    if (item.Title != null && item.Title.Contains(searchText))
                    {
                        filteredItems.Add(item);
                    }
                }

                // Clear the existing items in the ListView.
                Contacts.Clear();

                // Add the filtered items back to the ListView.
                foreach (CustomItem item in filteredItems)
                {
                    Contacts.Add(item);
                }
            }
        }


    }
}
