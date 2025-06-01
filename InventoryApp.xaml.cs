using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InventoryApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new OrderViewModel();
        }
    }

   class OrderViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Product> _products;
        private ObservableCollection<Product> _filteredProducts;
        private Product _selectedProduct;
        private bool _isOrderFormVisible;
        private string _productFilter;
        private string _selectedStatusFilter;
        private int _orderQuantity;
        private string _orderComment;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set { _filteredProducts = value; OnPropertyChanged(); }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public bool IsOrderFormVisible
        {
            get => _isOrderFormVisible;
            set { _isOrderFormVisible = value; OnPropertyChanged(); }
        }

        public string ProductFilter
        {
            get => _productFilter;
            set { _productFilter = value; OnPropertyChanged(); FilterProducts(); }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { _selectedStatusFilter = value; OnPropertyChanged(); FilterProducts(); }
        }

        public List<string> StatusFilters { get; } = new List<string> { "Всі", "В наявності", "Закінчується", "Закінчився" };

        public int OrderQuantity
        {
            get => _orderQuantity;
            set { _orderQuantity = value; OnPropertyChanged(); }
        }

        public string OrderComment
        {
            get => _orderComment;
            set { _orderComment = value; OnPropertyChanged(); }
        }

        public RelayCommand<Product> PlaceOrderCommand { get; }
        public RelayCommand ConfirmOrderCommand { get; }
        public RelayCommand CancelOrderCommand { get; }

        public OrderViewModel()
        {
            PlaceOrderCommand = new RelayCommand<Product>(PlaceOrder);
            ConfirmOrderCommand = new RelayCommand(ConfirmOrder);
            CancelOrderCommand = new RelayCommand(CancelOrder);
            SelectedStatusFilter = "Всі";

            LoadProducts();
        }

        private void LoadProducts()
        {
            // Приклад даних - замініть на реальні дані з бази
            Products = new ObservableCollection<Product>
            {
                new Product { Id = 1, Name = "Ноутбук HP", Category = "Електроніка", Quantity = 15, MinStock = 5, SupplierId = 1, SupplierName = "ЕлектронПостач" },
                new Product { Id = 2, Name = "Монітор Samsung", Category = "Електроніка", Quantity = 8, MinStock = 5, SupplierId = 1, SupplierName = "ЕлектронПостач" },
                new Product { Id = 3, Name = "Мишка Logitech", Category = "Аксесуари", Quantity = 3, MinStock = 10, SupplierId = 2, SupplierName = "ГаджетПостач" },
                new Product { Id = 4, Name = "Клавіатура Razer", Category = "Аксесуари", Quantity = 0, MinStock = 5, SupplierId = 2, SupplierName = "ГаджетПостач" },
                new Product { Id = 5, Name = "Навушники Sony", Category = "Аудіо", Quantity = 12, MinStock = 8, SupplierId = 3, SupplierName = "АудіоПостач" },
                new Product { Id = 6, Name = "Кабель USB-C", Category = "Аксесуари", Quantity = 2, MinStock = 15, SupplierId = 2, SupplierName = "ГаджетПостач" }
            };

            // Оновлення статусів
            foreach (var product in Products)
            {
                product.UpdateStatus();
            }

            FilteredProducts = new ObservableCollection<Product>(Products);
        }

        private void FilterProducts()
        {
            if (Products == null) return;

            var filtered = Products.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(ProductFilter))
            {
                filtered = filtered.Where(p =>
                    p.Name.Contains(ProductFilter, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(ProductFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedStatusFilter != "Всі")
            {
                filtered = filtered.Where(p => p.Status == SelectedStatusFilter);
            }

            FilteredProducts = new ObservableCollection<Product>(filtered);
        }

        private void PlaceOrder(Product product)
        {
            SelectedProduct = product;
            OrderQuantity = product.MinStock * 2; // Замовляємо вдвічі більше мінімального запасу
            OrderComment = $"Автоматичне замовлення для {product.Name}";
            IsOrderFormVisible = true;
        }

        private void ConfirmOrder()
        {
            if (OrderQuantity <= 0)
            {
                MessageBox.Show("Будь ласка, вкажіть коректну кількість товару", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Тут код для збереження замовлення в базі даних
            MessageBox.Show($"Замовлення на {SelectedProduct.Name} у кількості {OrderQuantity} шт. успішно створено!\nПостачальник: {SelectedProduct.SupplierName}",
                "Замовлення створено", MessageBoxButton.OK, MessageBoxImage.Information);

            // Оновлення кількості товару
            SelectedProduct.Quantity += OrderQuantity;
            SelectedProduct.UpdateStatus();

            // Оновлення фільтрації
            FilterProducts();

            // Закриття форми
            IsOrderFormVisible = false;
        }

        private void CancelOrder()
        {
            IsOrderFormVisible = false;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Product : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); UpdateStatus(); }
        }

        public int MinStock { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }

        private string _status;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        private Brush _statusColor;
        public Brush StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        private bool _canOrder;
        public bool CanOrder
        {
            get => _canOrder;
            set { _canOrder = value; OnPropertyChanged(); }
        }

        public void UpdateStatus()
        {
            if (Quantity == 0)
            {
                Status = "Закінчився";
                StatusColor = Brushes.Red;
                CanOrder = true;
            }
            else if (Quantity <= MinStock)
            {
                Status = "Закінчується";
                StatusColor = Brushes.Orange;
                CanOrder = true;
            }
            else
            {
                Status = "В наявності";
                StatusColor = Brushes.Green;
                CanOrder = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : System.Windows.Input.ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

        public void Execute(object parameter) => _execute((T)parameter);
    }
}