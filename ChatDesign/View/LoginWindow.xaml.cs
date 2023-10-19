using ChatDesign.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            //CallWindow a = new();
            //CallWindowServer b = new();
            //a.Show();
            //b.Show();

        }

        // Login 
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {

            string username = UserName.Text;
            string password = UserPassword.Password.ToString();
            bool authenticated = DbOperations.AuthenticateUser(username, password);

            if (authenticated)
            {
                // Successful Login
                MessageBox.Show("Login Successful");

                MainViewModel mainViewModel = new MainViewModel(username);
                // Login Default User 

                MainWindow a = new MainWindow();
                a.DataContext = mainViewModel;
                a.Closing += new CancelEventHandler(mainViewModel.MainWindow_Closing);
                a.Show();
                this.Close();
            }
            else
            {
                // User authentication failed
                MessageBox.Show("Failed Login");
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow a = new RegisterWindow();
            a.Show();
            this.Close();
        }
    }
}
