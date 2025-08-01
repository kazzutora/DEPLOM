﻿using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class ProductsWindow : Window
    {
        private List<Product> products;
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private UniformGrid itemsPanel;

        public ProductsWindow()
        {
            InitializeComponent();
            Loaded += ProductsWindow_Loaded;
            LoadCategories();
            LoadProductsFromDatabase();
            SizeChanged += Window_SizeChanged;
        }

        private void ProductsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            itemsPanel = FindVisualChild<UniformGrid>(ProductsListBox);
            UpdateColumns();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumns();
        }

        private void UpdateColumns()
        {
            if (itemsPanel != null && ProductsListBox.ActualWidth > 0)
            {
                double itemWidth = 200;
                int columns = Math.Max(1, (int)(ProductsListBox.ActualWidth / itemWidth));
                itemsPanel.Columns = columns;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
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
        p.PurchasePrice,
        s.Name AS SupplierName
    FROM 
        Products p
    LEFT JOIN 
        Categories c ON p.CategoryID = c.CategoryID
    LEFT JOIN 
        Suppliers s ON p.SupplierID = s.SupplierID";

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
                                    ImageBytes = reader["ImageBytes"] as byte[],
                                    SupplierName = reader["SupplierName"]?.ToString()
                                });
                            }
                        }
                    }
                }

                ProductsListBox.ItemsSource = products;
                UpdateColumns();
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
                var editWindow = new EditProductWindow(selectedProduct);
                if (editWindow.ShowDialog() == true)
                {
                    // Оновлюємо відображення
                    ProductsListBox.Items.Refresh();
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, виберіть продукт для редагування.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void Product_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                EditProduct_Click(sender, e);
            }
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
        private string _supplierName;
        public string SupplierName
        {
            get => _supplierName;
            set { _supplierName = value; OnPropertyChanged(nameof(SupplierName)); }
        }

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