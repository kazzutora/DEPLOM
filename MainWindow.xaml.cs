using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Windows;
using WpfApp1.Models;

namespace WpfApp1
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private decimal _dailySales;

        public decimal DailySales
        {
            get => _dailySales;
            set
            {
                _dailySales = value;
                OnPropertyChanged(nameof(DailySales));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += MainWindow_Loaded;
            UpdateDailySales();
            CheckPermissions();
            DisplayUserInfo();
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

            // Для всіх ролей
            BtnSales.Visibility = Visibility.Visible;
            MenuItemSales.Visibility = Visibility.Visible;

            switch (App.CurrentUser.RoleName)
            {
                case "Адміністратор":
                    // Повний доступ
                    break;

                case "Касир":
                    // Приховуємо адміністративні функції
                    MenuItemProgram.Visibility = Visibility.Collapsed;
                    BtnProducts.Visibility = Visibility.Collapsed;
                    BtnCategories.Visibility = Visibility.Collapsed;
                    BtnSuppliers.Visibility = Visibility.Collapsed;
                    BtnOrderManagement.Visibility = Visibility.Collapsed;
                    BtnAddProduct.Visibility = Visibility.Collapsed;
                    BtnViewReports.Visibility = Visibility.Collapsed;
                    MenuItemProducts.Visibility = Visibility.Collapsed;
                    MenuItemCategories.Visibility = Visibility.Collapsed;
                    MenuItemSuppliers.Visibility = Visibility.Collapsed;
                    break;

                case "Менеджер":
                    // Приховуємо деякі адміністративні функції
                    MenuItemFile.Visibility = Visibility.Collapsed;
                    MenuItemExit.Visibility = Visibility.Collapsed;
                    BtnOrderManagement.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        public void UpdateDailySales()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    DateTime today = DateTime.Today;
                    DateTime tomorrow = today.AddDays(1);

                    DailySales = context.Sales
                        .Include(s => s.Product)
                        .Where(s => s.SaleDate >= today && s.SaleDate < tomorrow)
                        .Sum(s => s.QuantitySold * s.Product.Price);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка розрахунку продажів: {ex.Message}");
                DailySales = 0;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme("Light");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentUser = null;
            new WpfApp1.Views.LoginWindow().Show();
            this.Close();
        }

        private void New_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Функція створення нового файлу");

        private void Open_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Функція відкриття файлу");

        private void Exit_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void OpenProducts_Click(object sender, RoutedEventArgs e)
            => new ProductsWindow().ShowDialog();

        private void OpenCategories_Click(object sender, RoutedEventArgs e)
            => new CategoriesWindow().ShowDialog();

        private void OpenSuppliers_Click(object sender, RoutedEventArgs e)
            => new SuppliersWindow().ShowDialog();

        private void OpenSales_Click(object sender, RoutedEventArgs e)
            => new SalesWindow().Show();

        private void OpenOrderManagementBtn_Click(object sender, RoutedEventArgs e)
            => new OrderManagementWindow().ShowDialog();

        private void LightTheme_Click(object sender, RoutedEventArgs e)
            => ApplyTheme("Light");

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
            => ApplyTheme("Dark");

        private void ApplyTheme(string themeName)
        {
            try
            {
                Application.Current.Resources.MergedDictionaries.Clear();

                var uri = new Uri($"/Themes/{themeName}Theme.xaml", UriKind.Relative);
                var themeDict = new ResourceDictionary { Source = uri };
                Application.Current.Resources.MergedDictionaries.Add(themeDict);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження теми: {ex.Message}");
            }
        }

        public void RefreshDashboard()
        {
            UpdateDailySales();
            OnPropertyChanged(nameof(DailySales));
        }

        // Швидкі дії
        private void AddProductQuick_Click(object sender, RoutedEventArgs e)
        {
            new AddEditProductWindow().ShowDialog();
            RefreshDashboard();
        }

        private void CreateSaleQuick_Click(object sender, RoutedEventArgs e)
        {
            new SalesWindow().Show();
        }

        private void ViewReports_Click(object sender, RoutedEventArgs e)
        {
            new SalesWindow().Show();
        }

        private void About_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("POS System v1.0\n© 2025");

        private void Home_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Головна сторінка");
    }
}