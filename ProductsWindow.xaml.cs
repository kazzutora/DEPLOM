using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
                            p.ProductID, 
                            p.Name, 
                            c.CategoryName AS Category, 
                            p.Quantity, 
                            p.Price, 
                            p.Image as ImageBytes,
                            p.PurchasePrice
                        FROM 
                            Products p
                        LEFT JOIN 
                            Categories c ON p.CategoryID = c.CategoryID";

                    if (!string.IsNullOrEmpty(category) && category != "Усі категорії")
                    {
                        query += " WHERE c.CategoryName = @Category";
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
                                products.Add(new Product
                                {
                                    ProductID = Convert.ToInt32(reader["ProductID"]),
                                    Name = reader["Name"].ToString(),
                                    Category = reader["Category"].ToString(),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    Price = Convert.ToDecimal(reader["Price"]),
                                    PurchasePrice = Convert.ToDecimal(reader["PurchasePrice"]),
                                    ImageBytes = reader["ImageBytes"] as byte[]
                                });
                            }
                        }
                    }
                }

                ProductsListBox.ItemsSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження продуктів: {ex.Message}");
            }
        }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            if (ProductsListBox.SelectedItem is Product selectedProduct)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = @"SELECT p.*, c.CategoryName, s.Name as SupplierName 
                                        FROM Products p
                                        LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                                        LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                                        WHERE p.ProductID = @ProductID";

                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ProductID", selectedProduct.ProductID);

                        DataTable dt = new DataTable();
                        dt.Load(command.ExecuteReader());

                        if (dt.Rows.Count > 0)
                        {
                            DataRowView row = dt.DefaultView[0];
                            AddEditProductWindow editWindow = new AddEditProductWindow(row);
                            bool? result = editWindow.ShowDialog();

                            if (result == true)
                            {
                                LoadProductsFromDatabase();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при редагуванні продукту: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, виберіть продукт для редагування.");
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsListBox.SelectedItem is Product selectedProduct)
            {
                try
                {
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
                            List<string> categories = new List<string> { "Усі категорії" };
                            while (reader.Read())
                            {
                                categories.Add(reader["CategoryName"].ToString());
                            }
                            CategoryFilterComboBox.ItemsSource = categories;
                            CategoryFilterComboBox.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження категорій: {ex.Message}");
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
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
        private byte[] _imageBytes;

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

        public byte[] ImageBytes
        {
            get => _imageBytes;
            set
            {
                _imageBytes = value;
                OnPropertyChanged(nameof(ImageBytes));
                OnPropertyChanged(nameof(ProductImage));
            }
        }

        public BitmapImage ProductImage
        {
            get
            {
                if (ImageBytes == null || ImageBytes.Length == 0)
                    return null;

                try
                {
                    var image = new BitmapImage();
                    using (var stream = new MemoryStream(ImageBytes))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                    }
                    return image;
                }
                catch
                {
                    return null;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}