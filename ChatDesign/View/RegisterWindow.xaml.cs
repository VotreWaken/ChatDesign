using ChatDesign.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChatDesign.View
{
    /// <summary>
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {

        // Register Window Constructor 
        public RegisterWindow()
        {
            InitializeComponent();
        }

        // Select File Click Event
        private void SelectFile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Cursor = System.Windows.Input.Cursors.Wait;
            this.SetFile();
            this.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        // Set File From User ( Call Dialog Window and Set UI To Changed Image )
        private void SetFile()
        {
            Microsoft.Win32.OpenFileDialog selectFile = new Microsoft.Win32.OpenFileDialog();

            selectFile.CheckFileExists = true;
            selectFile.Filter = "Image files (*.jpg*)|*.jpg*";
            if ((bool)selectFile.ShowDialog(this))
            {
                this.FileName.Text = selectFile.FileName;
            }
            ImagePresentation.Source = new BitmapImage(new Uri(selectFile.FileName));
        }

        // Upload New User Into Database
        private void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            // Store Image
            if (string.IsNullOrEmpty(this.FileName.Text))
            {
                System.Windows.MessageBox.Show("Select file to upload", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                return;
            }
            this.Cursor = System.Windows.Input.Cursors.Wait;
            switch (((System.Windows.Controls.Button)sender).Tag.ToString())
            {
                case "Traditional":
                    this.AverageTime1.Text = (DbOperations.StoreFileUsingSqlParameter(this.FileName.Text, DbOperations.TableType.Traditional).TotalMilliseconds).ToString();
                    break;
                case "FileStream1":
                    break;
                default:
                    System.Windows.MessageBox.Show("Invalid tag", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                    break;
            }
            this.Cursor = System.Windows.Input.Cursors.Arrow;

            // Store User
            DbOperations.StoreUserUsingSqlParameter(UserName.Text, UserPassword.Text, DbOperations.GetImageId(FileName.Text.Substring(FileName.Text.LastIndexOf('\\') + 1)), false, 0, 0);
            
            
            // Login New Register User 
            MainViewModel mainViewModel = new MainViewModel(UserName.Text);

            // Here Change 
            MainWindow a = new MainWindow();
            a.DataContext = mainViewModel;
            a.Closing += new CancelEventHandler(mainViewModel.MainWindow_Closing);
            a.Show();
            this.Close();
        }
    }
}
