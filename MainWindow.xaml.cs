using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApp1.Models;

namespace WpfApp1
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private decimal _dailySales;
        private decimal _weeklySales;
        private int _itemsSold;
        private string _topProduct;
        private readonly string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";

        public decimal DailySales
        {
            get => _dailySales;
            set
            {
                if (_dailySales != value)
                {
                    _dailySales = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AverageReceipt));
                }
            }
        }

        public decimal WeeklySales
        {
            get => _weeklySales;
            set
            {
                if (_weeklySales != value)
                {
                    _weeklySales = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ItemsSold
        {
            get => _itemsSold;
            set
            {
                if (_itemsSold != value)
                {
                    _itemsSold = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AverageReceipt));
                }
            }
        }

        public decimal AverageReceipt => ItemsSold > 0 ? DailySales / ItemsSold : 0;

        public string TopProduct
        {
            get => _topProduct;
            set
            {
                if (_topProduct != value)
                {
                    _topProduct = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainWindow()
        {
            LoadTheme();
            InitializeComponent();
            DataContext = this;
            Loaded += MainWindow_Loaded;
            CheckPermissions();
            DisplayUserInfo();
        }
        private void LoadTheme()
        {
            try
            {
                // Завантажити тему за замовчуванням
                var themeName = "Light"; // Можна зберігати в налаштуваннях
                ApplyTheme(themeName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження теми: {ex.Message}");
            }
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
            SetupAutoRefresh();
        }
        private void OpenWriteOffWindow_Click(object sender, RoutedEventArgs e)
        {
            new WriteOffWindow().ShowDialog();
        }
        private void LoadStatistics()
        {
            try
            {
                DailySales = GetDailySalesFromDatabase();
                WeeklySales = GetWeeklySalesFromDatabase();
                ItemsSold = GetSoldItemsCountFromDatabase();
                TopProduct = GetTopProductFromDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення статистики: {ex.Message}");
            }
        }

        private decimal GetDailySalesFromDatabase()
        {
            string query = @"SELECT ISNULL(SUM(TotalAmount), 0) 
                         FROM Sales 
                         WHERE CONVERT(date, SaleDate) = CONVERT(date, GETDATE())";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        private decimal GetWeeklySalesFromDatabase()
        {
            string query = @"SELECT ISNULL(SUM(TotalAmount), 0)
                         FROM Sales 
                         WHERE SaleDate >= DATEADD(day, -7, GETDATE()) 
                         AND SaleDate < DATEADD(day, 1, GETDATE())";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        private int GetSoldItemsCountFromDatabase()
        {
            string query = @"SELECT ISNULL(SUM(QuantitySold), 0) 
                         FROM Sales 
                         WHERE CONVERT(date, SaleDate) = CONVERT(date, GETDATE())";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private string GetTopProductFromDatabase()
        {
            string query = @"SELECT TOP 1 p.Name
                         FROM Sales s
                         JOIN Products p ON s.ProductID = p.ProductID
                         WHERE CONVERT(date, s.SaleDate) = CONVERT(date, GETDATE())
                         GROUP BY p.Name
                         ORDER BY SUM(s.QuantitySold) DESC";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? result.ToString() : "Немає даних";
            }
        }

        private DispatcherTimer _refreshTimer;
        private void SetupAutoRefresh()
        {
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromMinutes(5);
            _refreshTimer.Tick += (s, e) => LoadStatistics();
            _refreshTimer.Start();
        }

        private void DisplayUserInfo()
        {
            if (App.CurrentUser != null)
            {
                UserInfoText.Text = App.CurrentUser.Username;
                UserRoleText.Text = App.CurrentUser.RoleName;
            }
        }

        private void CheckPermissions()
        {
            if (App.CurrentUser == null)
            {
                MessageBox.Show("Користувач не авторизований");
                Close();
                return;
            }

            BtnSales.Visibility = Visibility.Visible;

            switch (App.CurrentUser.RoleName)
            {
                case "Адміністратор":
                    break;
                case "Касир":
                    BtnProducts.Visibility = Visibility.Collapsed;
                    BtnCategories.Visibility = Visibility.Collapsed;
                    BtnSuppliers.Visibility = Visibility.Collapsed;
                    BtnOrderManagement.Visibility = Visibility.Collapsed;
                    BtnAddProduct.Visibility = Visibility.Collapsed;
                    break;
                case "Менеджер":
                    BtnOrderManagement.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentUser = null;
            new WpfApp1.Views.LoginWindow().Show();
            this.Close();
        }


        private void OpenProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new ProductsWindow();
                window.Owner = this;
                window.Closed += (s, args) => RefreshAllStatistics();
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відкритті вікна продуктів: {ex.Message}\n\nДеталі:\n{ex.InnerException?.Message}");
            }
        }

        private void OpenCategories_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new CategoriesWindow();
                window.Owner = this;
                window.Closed += (s, args) => RefreshAllStatistics();
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відкритті вікна категорій: {ex.Message}\n\nДеталі:\n{ex.InnerException?.Message}");
            }
        }

        private void OpenSuppliers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new SuppliersWindow();
                window.Owner = this;
                window.Closed += (s, args) => RefreshAllStatistics();
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відкритті вікна постачальників: {ex.Message}\n\nДеталі:\n{ex.InnerException?.Message}");
            }
        }

        private void OpenSales_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new SalesWindow();
                window.Owner = this;
                window.Closed += (s, args) => RefreshAllStatistics();
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відкритті вікна продажів: {ex.Message}\n\nДеталі:\n{ex.InnerException?.Message}");
            }
        }

        private void OpenOrderManagementBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new OrderManagementWindow();
                window.Owner = this;
                window.Closed += (s, args) => RefreshAllStatistics();
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відкритті вікна управління замовленнями: {ex.Message}\n\nДеталі:\n{ex.InnerException?.Message}");
            }
        }

        private void ApplyTheme(string themeName)
        {
            try
            {
                Application.Current.Resources.MergedDictionaries.Clear();

                var uri = new Uri($"/Themes/{themeName}Theme.xaml", UriKind.Relative);
                var themeDict = new ResourceDictionary { Source = uri };
                Application.Current.Resources.MergedDictionaries.Add(themeDict);

                AddFallbackResources();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження теми: {ex.Message}");
            }
        }
        private void AddFallbackResources()
        {
            // Додати резервні ресурси для ключів, які можуть бути відсутніми
            if (!Application.Current.Resources.Contains("BorderBrush"))
            {
                Application.Current.Resources.Add("BorderBrush", new SolidColorBrush(Colors.Gray));
            }

            if (!Application.Current.Resources.Contains("ControlBackground"))
            {
                Application.Current.Resources.Add("ControlBackground", new SolidColorBrush(Colors.White));
            }

            if (!Application.Current.Resources.Contains("TextBrush"))
            {
                Application.Current.Resources.Add("TextBrush", new SolidColorBrush(Colors.Black));
            }
        }
        public void RefreshAllStatistics()
        {
            LoadStatistics();
        }
        private void ApplyLightTheme()
        {
            Resources["PrimaryColor"] = Color.FromRgb(0x6F, 0x42, 0xC1);
            Resources["TextColor"] = Colors.Black;
            Resources["CardColor"] = Colors.White;
            Resources["ShadowColor"] = Color.FromArgb(0x40, 0x00, 0x00, 0x00);
        }

        private void ApplyDarkTheme()
        {
            Resources["PrimaryColor"] = Color.FromRgb(0x9A, 0x6C, 0xFF);
            Resources["TextColor"] = Colors.White;
            Resources["CardColor"] = Color.FromRgb(0x33, 0x33, 0x38);
            Resources["ShadowColor"] = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF);
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e) => ApplyLightTheme();
        private void DarkTheme_Click(object sender, RoutedEventArgs e) => ApplyDarkTheme();

        private void AddProductQuick_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new AddEditProductWindow();
                window.Owner = this;
                window.ShowDialog();
                LoadStatistics(); // Оновлюємо статистику після закриття вікна
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відкритті вікна додавання продукту: {ex.Message}");
            }
        }

        private void CreateSaleQuick_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new SalesWindow();
                window.Owner = this;
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відкритті вікна продажів: {ex.Message}");
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("POS System v1.0\n© 2025");
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics(); // Оновлюємо всю статистику
        }
    }
}