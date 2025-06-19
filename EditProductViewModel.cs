using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public class EditProductViewModel : INotifyPropertyChanged
    {
        private readonly string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private readonly Product originalProduct;
        private byte[] imageBytes;
        private readonly ErrorProvider errorProvider = new ErrorProvider();

        public EditProductViewModel(Product product)
        {
            originalProduct = product ?? throw new ArgumentNullException(nameof(product));
            Name = product.Name;
            SelectedCategory = product.Category;
            Quantity = product.Quantity;
            Price = product.Price;
            PurchasePrice = product.PurchasePrice;
            ImageBytes = product.ImageBytes;
            ProductImage = product.ProductImage;

            LoadCategoriesAndSuppliers();
            SaveCommand = new RelayCommand(Save, CanSave);
        }

        public ICommand SaveCommand { get; }

        public List<string> Categories { get; private set; } = new List<string>();
        public List<string> Suppliers { get; private set; } = new List<string>();

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
                ValidateProperty(nameof(Name), value);
            }
        }

        private string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
                ValidateProperty(nameof(SelectedCategory), value);
            }
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                ValidateProperty(nameof(Quantity), value);
            }
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
                ValidateProperty(nameof(Price), value);
            }
        }

        private string _selectedSupplier;
        public string SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                OnPropertyChanged(nameof(SelectedSupplier));
                ValidateProperty(nameof(SelectedSupplier), value);
            }
        }

        private decimal _purchasePrice;
        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set
            {
                _purchasePrice = value;
                OnPropertyChanged(nameof(PurchasePrice));
                ValidateProperty(nameof(PurchasePrice), value);
            }
        }

        public byte[] ImageBytes
        {
            get => imageBytes;
            set
            {
                imageBytes = value;
                ProductImage = LoadImage(imageBytes);
                OnPropertyChanged(nameof(ProductImage));
            }
        }

        private BitmapImage _productImage;
        public BitmapImage ProductImage
        {
            get => _productImage;
            private set { _productImage = value; OnPropertyChanged(nameof(ProductImage)); }
        }

        // Властивості для помилок
        public string NameError => errorProvider.GetError(nameof(Name));
        public string CategoryError => errorProvider.GetError(nameof(SelectedCategory));
        public string QuantityError => errorProvider.GetError(nameof(Quantity));
        public string PriceError => errorProvider.GetError(nameof(Price));
        public string SupplierError => errorProvider.GetError(nameof(SelectedSupplier));
        public string PurchasePriceError => errorProvider.GetError(nameof(PurchasePrice));

        public bool HasErrors => errorProvider.HasErrors;

        private void ValidateProperty(string propertyName, object value)
        {
            switch (propertyName)
            {
                case nameof(Name):
                    if (!Validator.ValidateName(Name))
                    {
                        errorProvider.SetError(propertyName, "Назва має містити тільки літери, цифри та пробіли");
                    }
                    else
                    {
                        errorProvider.ClearError(propertyName);
                    }
                    OnPropertyChanged(nameof(NameError));
                    break;

                case nameof(Quantity):
                    if (!Validator.ValidateNumber(Quantity.ToString(), false, 0, 10000))
                    {
                        errorProvider.SetError(propertyName, "Кількість має бути цілим числом від 0 до 10000");
                    }
                    else
                    {
                        errorProvider.ClearError(propertyName);
                    }
                    OnPropertyChanged(nameof(QuantityError));
                    break;

                case nameof(Price):
                    if (!Validator.ValidateNumber(Price.ToString(), true, 0.01, 1000000))
                    {
                        errorProvider.SetError(propertyName, "Ціна має бути числом від 0.01 до 1 000 000");
                    }
                    else
                    {
                        errorProvider.ClearError(propertyName);
                    }
                    OnPropertyChanged(nameof(PriceError));
                    break;

                case nameof(PurchasePrice):
                    if (!Validator.ValidateNumber(PurchasePrice.ToString(), true, 0.01, 1000000))
                    {
                        errorProvider.SetError(propertyName, "Ціна закупівлі має бути числом від 0.01 до 1 000 000");
                    }
                    else
                    {
                        errorProvider.ClearError(propertyName);
                    }
                    OnPropertyChanged(nameof(PurchasePriceError));
                    break;

                case nameof(SelectedCategory):
                    if (string.IsNullOrEmpty(SelectedCategory))
                    {
                        errorProvider.SetError(propertyName, "Будь ласка, оберіть категорію");
                    }
                    else
                    {
                        errorProvider.ClearError(propertyName);
                    }
                    OnPropertyChanged(nameof(CategoryError));
                    break;

                case nameof(SelectedSupplier):
                    if (string.IsNullOrEmpty(SelectedSupplier))
                    {
                        errorProvider.SetError(propertyName, "Будь ласка, оберіть постачальника");
                    }
                    else
                    {
                        errorProvider.ClearError(propertyName);
                    }
                    OnPropertyChanged(nameof(SupplierError));
                    break;
            }

            // Оновлюємо стан команди збереження
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }

        private void LoadCategoriesAndSuppliers()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Завантаження категорій
                    var categoryQuery = "SELECT CategoryName FROM Categories";
                    using (var command = new SqlCommand(categoryQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Categories.Add(reader["CategoryName"].ToString());
                        }
                    }

                    // Завантаження постачальників
                    var supplierQuery = "SELECT Name FROM Suppliers";
                    using (var command = new SqlCommand(supplierQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Suppliers.Add(reader["Name"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження даних: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadImageFromFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Зображення (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp",
                Title = "Виберіть зображення продукту"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    ImageBytes = File.ReadAllBytes(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка завантаження зображення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

            try
            {
                var image = new BitmapImage();
                using (var mem = new MemoryStream(imageData))
                {
                    mem.Position = 0;
                    image.BeginInit();
                    image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = null;
                    image.StreamSource = mem;
                    image.EndInit();
                }
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private bool CanSave(object parameter)
        {
            return !errorProvider.HasErrors &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   SelectedCategory != null &&
                   Quantity >= 0 &&
                   Price > 0 &&
                   SelectedSupplier != null &&
                   PurchasePrice > 0;
        }

        private void Save(object parameter)
        {
            if (errorProvider.HasErrors)
            {
                MessageBox.Show("Виправте помилки перед збереженням", "Помилки валідації",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var query = @"UPDATE Products SET
                                  Name = @Name,
                                  CategoryID = (SELECT CategoryID FROM Categories WHERE CategoryName = @Category),
                                  Quantity = @Quantity,
                                  Price = @Price,
                                  SupplierID = (SELECT SupplierID FROM Suppliers WHERE Name = @Supplier),
                                  Image = @Image,
                                  PurchasePrice = @PurchasePrice
                              WHERE ProductID = @ProductID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", Name);
                        command.Parameters.AddWithValue("@Category", SelectedCategory);
                        command.Parameters.AddWithValue("@Quantity", Quantity);
                        command.Parameters.AddWithValue("@Price", Price);
                        command.Parameters.AddWithValue("@Supplier", SelectedSupplier);
                        command.Parameters.AddWithValue("@PurchasePrice", PurchasePrice);
                        command.Parameters.AddWithValue("@Image", ImageBytes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ProductID", originalProduct.ProductID);

                        command.ExecuteNonQuery();
                    }
                }

                // Оновлюємо оригінальний продукт
                originalProduct.Name = Name;
                originalProduct.Category = SelectedCategory;
                originalProduct.Quantity = Quantity;
                originalProduct.Price = Price;
                originalProduct.PurchasePrice = PurchasePrice;
                originalProduct.ImageBytes = ImageBytes;

                if (Application.Current.Windows.OfType<EditProductWindow>().Any())
                {
                    Application.Current.Windows.OfType<EditProductWindow>().First().DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}