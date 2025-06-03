using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class AddEditProductWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private DataRowView editingProduct;
        private string productImagePath;

        public AddEditProductWindow(DataRowView product = null)
        {
            InitializeComponent();
            editingProduct = product;
            LoadCategoriesAndSuppliers();

            if (editingProduct != null)
            {
                // If editing an existing product, populate fields
                NameTextBox.Text = editingProduct["Name"].ToString();
                CategoryComboBox.SelectedItem = editingProduct["Category"].ToString();
                QuantityTextBox.Text = editingProduct["Quantity"].ToString();
                PriceTextBox.Text = editingProduct["Price"].ToString();
                SupplierComboBox.SelectedItem = editingProduct["Supplier"].ToString();
                PurchasePriceTextBox.Text = editingProduct["PurchasePrice"].ToString();

                // If there is an image associated with the product, display it
                if (editingProduct["Image"] != DBNull.Value)
                {
                    byte[] imageData = (byte[])editingProduct["Image"];
                    BitmapImage bitmap = new BitmapImage();
                    using (MemoryStream stream = new MemoryStream(imageData))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }
                    ProductImage.Source = bitmap;
                }
            }
        }
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Створюємо BitmapImage для коректного завантаження
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Для безпечного використання в UI

                    ProductImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка завантаження зображення: {ex.Message}");
                }
            }
        }


        // Loading categories and suppliers into ComboBox
        private void LoadCategoriesAndSuppliers()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Load Categories
                    string categoryQuery = "SELECT CategoryName FROM Categories";
                    SqlCommand categoryCommand = new SqlCommand(categoryQuery, connection);
                    SqlDataReader categoryReader = categoryCommand.ExecuteReader();
                    while (categoryReader.Read())
                    {
                        CategoryComboBox.Items.Add(categoryReader["CategoryName"].ToString());
                    }
                    categoryReader.Close();

                    // Load Suppliers
                    string supplierQuery = "SELECT Name FROM Suppliers";
                    SqlCommand supplierCommand = new SqlCommand(supplierQuery, connection);
                    SqlDataReader supplierReader = supplierCommand.ExecuteReader();
                    while (supplierReader.Read())
                    {
                        SupplierComboBox.Items.Add(supplierReader["Name"].ToString());
                    }
                    supplierReader.Close();
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Error loading data: " + ex.Message);
                }
            }
        }

        // Save button handler
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                CategoryComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(QuantityTextBox.Text) ||
                string.IsNullOrWhiteSpace(PriceTextBox.Text) ||
                SupplierComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query;
                    if (editingProduct == null)
                    {
                        // Adding new product
                        query = @"
                   INSERT INTO Products (Name, CategoryID, Quantity, Price, SupplierID, ImagePath, PurchasePrice)
VALUES (@Name, 
    (SELECT TOP 1 CategoryID FROM Categories WHERE CategoryName = @Category), 
    @Quantity, 
    @Price, 
    (SELECT SupplierID FROM Suppliers WHERE Name = @Supplier), 
    @ImagePath, 
    @PurchasePrice)";
                    }
                    else
                    {
                        // Updating existing product
                        query = @"
                  UPDATE Products
SET 
    Name = @Name,
    CategoryID = (SELECT CategoryID FROM Categories WHERE CategoryName = @Category),
    Quantity = @Quantity,
    Price = @Price,
    SupplierID = (SELECT SupplierID FROM Suppliers WHERE Name = @Supplier),
    ImagePath = @ImagePath,
    PurchasePrice = @PurchasePrice
WHERE ProductID = @ProductID";
                    }

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Name", NameTextBox.Text);
                    command.Parameters.AddWithValue("@Category", CategoryComboBox.SelectedItem.ToString());
                    command.Parameters.AddWithValue("@Quantity", int.Parse(QuantityTextBox.Text));
                    command.Parameters.AddWithValue("@Price", decimal.Parse(PriceTextBox.Text));
                    command.Parameters.AddWithValue("@Supplier", SupplierComboBox.SelectedItem.ToString());
                    command.Parameters.AddWithValue("@PurchasePrice", decimal.Parse(PurchasePriceTextBox.Text));

                    // Store the image path instead of the image bytes
                    command.Parameters.AddWithValue("@ImagePath", string.IsNullOrWhiteSpace(productImagePath) ? (object)DBNull.Value : productImagePath);

                    if (editingProduct != null)
                    {
                        command.Parameters.AddWithValue("@ProductID", (int)editingProduct["ProductID"]);
                    }

                    command.ExecuteNonQuery();
                    DialogResult = true;
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Save error: " + ex.Message);
                }
            }
        }


        // Cancel button handler
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // Add Image Button Click handler
        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
            if (openFileDialog.ShowDialog() == true)
            {
                productImagePath = openFileDialog.FileName;

                // Display the selected image in the Image control
                ProductImage.Source = new BitmapImage(new Uri(productImagePath));
            }
        }
    }
}
