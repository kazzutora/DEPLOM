using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class EditProductWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private Product product;
        private byte[] imageBytes;

        public EditProductWindow(Product product)
        {
            InitializeComponent();
            this.product = product;
            LoadCategoriesAndSuppliers();
            LoadProductData();
        }

        private void LoadProductData()
        {
            NameTextBox.Text = product.Name;
            QuantityTextBox.Text = product.Quantity.ToString();
            PriceTextBox.Text = product.Price.ToString();
            PurchasePriceTextBox.Text = product.PurchasePrice.ToString();
            CategoryComboBox.SelectedItem = product.Category;
            SupplierComboBox.SelectedItem = product.SupplierName; // Припустимо, що у продукта є SupplierName

            if (product.ImageBytes != null && product.ImageBytes.Length > 0)
            {
                imageBytes = product.ImageBytes;
                ProductImage.Source = LoadImage(imageBytes);
            }
        }

        private BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

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
                            CategoryComboBox.Items.Add(reader["CategoryName"].ToString());
                        }
                    }

                    // Завантаження постачальників
                    var supplierQuery = "SELECT Name FROM Suppliers";
                    using (var command = new SqlCommand(supplierQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SupplierComboBox.Items.Add(reader["Name"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження даних: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
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
                    imageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    ProductImage.Source = LoadImage(imageBytes);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка завантаження зображення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валідація
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                CategoryComboBox.SelectedItem == null ||
                !int.TryParse(QuantityTextBox.Text, out int quantity) ||
                !decimal.TryParse(PriceTextBox.Text, out decimal price) ||
                SupplierComboBox.SelectedItem == null ||
                !decimal.TryParse(PurchasePriceTextBox.Text, out decimal purchasePrice))
            {
                MessageBox.Show("Будь ласка, перевірте правильність введених даних.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        command.Parameters.AddWithValue("@Name", NameTextBox.Text);
                        command.Parameters.AddWithValue("@Category", CategoryComboBox.SelectedItem.ToString());
                        command.Parameters.AddWithValue("@Quantity", quantity);
                        command.Parameters.AddWithValue("@Price", price);
                        command.Parameters.AddWithValue("@Supplier", SupplierComboBox.SelectedItem.ToString());
                        command.Parameters.AddWithValue("@PurchasePrice", purchasePrice);
                        command.Parameters.AddWithValue("@Image", imageBytes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ProductID", product.ProductID);

                        command.ExecuteNonQuery();
                    }
                }

                // Оновлюємо продукт у списку
                product.Name = NameTextBox.Text;
                product.Category = CategoryComboBox.SelectedItem.ToString();
                product.Quantity = quantity;
                product.Price = price;
                product.PurchasePrice = purchasePrice;
                product.ImageBytes = imageBytes;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}