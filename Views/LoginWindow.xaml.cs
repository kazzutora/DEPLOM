// Views/LoginWindow.xaml.cs
using WpfApp1.Services;
using System.Windows;
using WpfApp1;

namespace WpfApp1.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService = new AuthService();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
           var user = _authService.Login(
                txtUsername.Text,
                txtPassword.Password
            );

            if (user != null)
            {
                new MainWindow().Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Невірні дані!");
            }
        }
    }
}