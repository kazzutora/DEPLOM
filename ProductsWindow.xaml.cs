using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

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
                        Products.ImagePath 
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
                if (MessageBox.Show($"Are you sure you want to delete the product '{selectedProduct.Name}'?",
                                    "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            string deleteQuery = "DELETE FROM Products WHERE ProductID = @ProductID";

                            using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                            {
                                command.Parameters.AddWithValue("@ProductID", selectedProduct.ProductID);
                                command.ExecuteNonQuery();
                            }
                        }

                        products.Remove(selectedProduct);
                        ProductsListBox.Items.Refresh();

                        MessageBox.Show("Product deleted successfully.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting product: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a product to delete.");
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

    public class Product
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ImagePath { get; set; }

        public BitmapImage ProductImage =>
            !string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath)
                ? new BitmapImage(new Uri(ImagePath))
                : null;
    }
}