using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Windows;

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
                        .Include(s => s.Product) // Підвантажуємо пов'язані продукти
                        .Where(s => s.SaleDate >= today && s.SaleDate < tomorrow)
                        .Sum(s => s.QuantitySold * s.Product.Price); // Використовуємо Price з продукту
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
            // Завантажити тему за замовчуванням
            ApplyTheme("Light");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            new ProductsWindow().ShowDialog();
            RefreshDashboard(); // Оновлюємо дані після додавання
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