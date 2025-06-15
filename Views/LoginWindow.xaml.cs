using System.Windows;
using WpfApp1.Services;
using WpfApp1.Models;

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
            string username = txtUsername.Text;
            string password = txtPassword.Password;

            User user = _authService.Login(username, password);

            if (user != null)
            {
                App.CurrentUser = user;
                new MainWindow().Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Невірний логін або пароль");
            }
        }
    }
}