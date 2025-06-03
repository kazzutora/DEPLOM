using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.ComponentModel;

namespace WpfApp1
{
    public partial class ProductsWindow : Window
    {
        private List<Product> products;
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";

        public ProductsWindow()
        {
            InitializeComponent();
            LoadCategories();
            LoadProductsFromDatabase();
        }

        private void LoadProductsFromDatabase(string category = null)
        {
            products = new List<Product>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                      SELECT 
                Products.ProductID, 
                Products.Name, 
                Categories.CategoryName AS Category, 
                Products.Quantity, 
                Products.Price, 
                Products.ImagePath,  
                Products.PurchasePrice
            FROM 
                Products
            LEFT JOIN 
                Categories ON Products.CategoryID = Categories.CategoryID";

                    if (!string.IsNullOrEmpty(category) && category != "Усі категорії")
                    {
                        query += " WHERE Categories.CategoryName = @Category";
                    }

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(category) && category != "Усі категорії")
                        {
                            command.Parameters.AddWithValue("@Category", category);
                        }

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string imagePath = reader["ImagePath"].ToString();
                                products.Add(new Product
                                {
                                    ProductID = Convert.ToInt32(reader["ProductID"]),
                                    Name = reader["Name"].ToString(),
                                    Category = reader["Category"].ToString(),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    PurchasePrice = Convert.ToDecimal(reader["PurchasePrice"]),
                                    Price = Convert.ToDecimal(reader["Price"]),
                                    ImagePath = File.Exists(imagePath) ? imagePath : null
                                });
                            }
                        }
                    }
                }

                ProductsListBox.ItemsSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}");
            }
        }


        private void CategoryFilterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CategoryFilterComboBox.SelectedItem != null)
            {
                LoadProductsFromDatabase(CategoryFilterComboBox.SelectedItem.ToString());
            }
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            AddEditProductWindow addEditProductWindow = new AddEditProductWindow();
            bool? result = addEditProductWindow.ShowDialog();

            if (result == true)
            {
                LoadProductsFromDatabase();
            }
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {

        }


        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsListBox.SelectedItem is Product selectedProduct)
            {
                try
                {
                    // Проверяем есть ли связанные заказы
                    if (HasRelatedOrders(selectedProduct.ProductID))
                    {
                        MessageBox.Show("Неможливо видалити продукт, оскільки існують пов'язані замовлення.",
                                        "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "DELETE FROM Products WHERE ProductID = @ProductID";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@ProductID", selectedProduct.ProductID);
                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Продукт успішно видалено!");
                                LoadProductsFromDatabase();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при видаленні продукту: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, виберіть продукт для видалення.");
            }
        }

        private bool HasRelatedOrders(int productId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Orders WHERE ProductID = @ProductID";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductID", productId);
                    int orderCount = (int)command.ExecuteScalar();
                    return orderCount > 0;
                }
            }
        }
        private void LoadCategories()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT DISTINCT CategoryName FROM Categories";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            List<string> categories = new List<string> { "Усі категорії" }; // Додаємо загальний пункт
                            while (reader.Read())
                            {
                                categories.Add(reader["CategoryName"].ToString());
                            }
                            CategoryFilterComboBox.ItemsSource = categories;
                            CategoryFilterComboBox.SelectedIndex = 0; // Вибираємо перший елемент
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження категорій: {ex.Message}");
            }
        }
        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Якщо у тебе є список продуктів, можна фільтрувати їх тут
            string filter = SearchTextBox.Text.ToLower();
            ProductsListBox.ItemsSource = products.Where(p => p.Name.ToLower().Contains(filter)).ToList();
        }



    }

    public class Product : INotifyPropertyChanged
    {
        private int _productID;
        private string _name;
        private string _category;
        private int _quantity;
        private decimal _price;
        private string _description;
        private decimal _purchasePrice;
        private string _imagePath;

        public int ProductID
        {
            get => _productID;
            set { _productID = value; OnPropertyChanged(nameof(ProductID)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set { _purchasePrice = value; OnPropertyChanged(nameof(PurchasePrice)); }
        }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                OnPropertyChanged(nameof(ImagePath));
                OnPropertyChanged(nameof(ProductImage)); // Важливо!
            }
        }

        public BitmapImage ProductImage
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath))
                {
                    return new BitmapImage(new Uri(ImagePath));
                }
                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}