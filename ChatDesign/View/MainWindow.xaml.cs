using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChatDesign.Model;
using ChatDesign.Control;
using ChatDesign.View;

namespace ChatDesign
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private void MyScrollViewer_ViewChanged(object sender,Control.MyScrollViewer e)
        {
            var scrollViewer = (ScrollViewer)sender;

            // You can determine when to load more data based on your requirements
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
            {
                // Load more data here
                //YourIncrementalLoadingCollectionInstance.LoadMoreItemsAsync(20); // Load 20 more items as an example
            }
        }
        public MainWindow(string username, byte[] image)
        {
            InitializeComponent();
            DataContext = new MainViewModel();


            Bitmap bitmapavatar = GetImageFromByteArray(image);
            UserAvatar.ImageSource = ImageSourceFromBitmap(bitmapavatar);
            Username.Content = username;


            foreach (var item in DbOperations.GetUserChats(DbOperations.GetUserId(username)))
            {
                Bitmap bitmap = GetImageFromByteArray(item.Avatar);
                Contacts.Items.Add(new CustomItem { ImagePath = ImageSourceFromBitmap(bitmap), Title = item.ChatName, Id = item.ID });
            }
            SaveOriginalContacts();
            // Initialize ChatMessages
            ChatMessages = new ObservableCollection<ChatItem>();
        }
        private void SendMessage(object sender, RoutedEventArgs e)
        {
            //MainViewModel viewModel = (MainViewModel)DataContext;
            //MainViewModel viewModel = (MainViewModel)DataContext;
            ChatItem i = new ChatItem { Content = MessageInput.Text, IsSender = true, Sender = Username.Content.ToString() };
            //ChatListBox.Items.Add(i);
            ChatMessages.Add(i);
        }
        private void ContactItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the selected item
            CustomItem selectedItem = (CustomItem)Contacts.SelectedItem;

            // Check if an item is selected
            if (selectedItem != null)
            {
                // Access your ViewModel (replace YourViewModel with your actual ViewModel type)
                MainViewModel viewModel = (MainViewModel)DataContext;
                viewModel.Username = Username.Content.ToString();
                viewModel.LoadMessages(((CustomItem)Contacts.SelectedItem).Id);
                // Invoke the ConnectCommand with the selected item as a parameter
                if (viewModel.ConnectCommand.CanExecute(selectedItem))
                {
                    viewModel.ConnectCommand.Execute(selectedItem);
                }
            }
            //DataContext = this;
            int chatID = ((CustomItem)Contacts.SelectedItem).Id;

            // Load chat messages for the selected contact
            ObservableCollection<ChatItem> chatMessages = DbOperations.LoadChatMessagesFromDatabase(chatID);

            // Clear and update the ChatListBox
            ChatMessages.Clear();
            foreach (var message in chatMessages)
            {
                ChatMessages.Add(message);
            }
        }

        public MainWindow()
        {
            DataContext = this; // Set DataContext to the MainWindow instance
        }

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

        private void ShowLeftPanel(object sender, RoutedEventArgs e)
        {
            var showAnimation = FindResource("ShowLeftPanelAnimation") as Storyboard;
            showAnimation?.Begin();
        }

        private void HideLeftPanel(object sender, RoutedEventArgs e)
        {
            var hideAnimation = FindResource("HideLeftPanelAnimation") as Storyboard;
            hideAnimation?.Begin();
        }

        private void ReturnLeftPanel(object sender, RoutedEventArgs e)
        {
            var showAnimation = FindResource("ShowLeftPanelAnimation") as Storyboard;
            showAnimation?.Begin();
        }


        private void SaveOriginalContacts()
        {
            for (int i = 0; i < Contacts.Items.Count; i++)
            {
                originalItems.Add((CustomItem)Contacts.Items[i]);
            }
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = Tb_ContactsSearch.Text; // Replace YourTextBox with your TextBox's name

            if (string.IsNullOrWhiteSpace(searchText))
            {
                Contacts.Items.Clear();
                // If the search text is empty, no filtering is needed, so you can leave the original items as they are.
                for (int i = 0; i < originalItems.Count; i++)
                {
                    Contacts.Items.Add((CustomItem)originalItems[i]);
                }
            }
            else
            {
                // Create a filtered list to store the items that match the search text.
                List<CustomItem> filteredItems = new List<CustomItem>();

                foreach (CustomItem item in Contacts.Items)
                {
                    if (item.Title != null && item.Title.Contains(searchText))
                    {
                        filteredItems.Add(item);
                    }
                }

                // Clear the existing items in the ListView.
                Contacts.Items.Clear();

                // Add the filtered items back to the ListView.
                foreach (CustomItem item in filteredItems)
                {
                    Contacts.Items.Add(item);
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = Tb_ContactsSearch.Text;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                List<User> searchResults = DbOperations.SearchUsers(searchText);
                MessageBox.Show(searchResults.Count.ToString());
                // Update your UI (e.g., a ListView or ListBox) with the search results.
                Contacts.Items.Clear();
                foreach (User user in searchResults)
                {
                    Bitmap bitmap = GetImageFromByteArray(user.ImagePath);
                    Contacts.Items.Add(new CustomItem { ImagePath = ImageSourceFromBitmap(bitmap), Title = user.Name });
                }
            }
            else
            {
                // Handle the case when the search text is empty (e.g., show all users).
                // You can reload the original list of users or display a message.
            }
        }
    }
}
