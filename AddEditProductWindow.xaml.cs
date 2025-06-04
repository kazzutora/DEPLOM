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
        private byte[] productImageBytes;

        public AddEditProductWindow(DataRowView product = null)
        {
            InitializeComponent();
            editingProduct = product;
            LoadCategoriesAndSuppliers();

            if (editingProduct != null)
            {
                NameTextBox.Text = editingProduct["Name"].ToString();
                CategoryComboBox.SelectedItem = editingProduct["Category"].ToString();
                QuantityTextBox.Text = editingProduct["Quantity"].ToString();
                PriceTextBox.Text = editingProduct["Price"].ToString();
                SupplierComboBox.SelectedItem = editingProduct["Supplier"].ToString();
                PurchasePriceTextBox.Text = editingProduct["PurchasePrice"].ToString();

                if (editingProduct["Image"] != DBNull.Value)
                {
                    productImageBytes = (byte[])editingProduct["Image"];
                    ProductImage.Source = LoadImage(productImageBytes);
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

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    productImageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    ProductImage.Source = LoadImage(productImageBytes);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка завантаження зображення: {ex.Message}");
                }
            }
        }

        private void LoadCategoriesAndSuppliers()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string categoryQuery = "SELECT CategoryName FROM Categories";
                    SqlCommand categoryCommand = new SqlCommand(categoryQuery, connection);
                    SqlDataReader categoryReader = categoryCommand.ExecuteReader();
                    while (categoryReader.Read())
                    {
                        CategoryComboBox.Items.Add(categoryReader["CategoryName"].ToString());
                    }
                    categoryReader.Close();

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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                CategoryComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(QuantityTextBox.Text) ||
                string.IsNullOrWhiteSpace(PriceTextBox.Text) ||
                SupplierComboBox.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, заповніть всі обов'язкові поля.");
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity))
            {
                MessageBox.Show("Будь ласка, введіть коректну кількість.");
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price))
            {
                MessageBox.Show("Будь ласка, введіть коректну ціну.");
                return;
            }

            if (!decimal.TryParse(PurchasePriceTextBox.Text, out decimal purchasePrice))
            {
                MessageBox.Show("Будь ласка, введіть коректну ціну закупівлі.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = editingProduct == null ?
                        @"INSERT INTO Products (Name, CategoryID, Quantity, Price, SupplierID, Image, PurchasePrice)
                          VALUES (@Name, 
                              (SELECT CategoryID FROM Categories WHERE CategoryName = @Category), 
                              @Quantity, 
                              @Price, 
                              (SELECT SupplierID FROM Suppliers WHERE Name = @Supplier), 
                              @Image, 
                              @PurchasePrice)" :
                        @"UPDATE Products SET
                          Name = @Name,
                          CategoryID = (SELECT CategoryID FROM Categories WHERE CategoryName = @Category),
                          Quantity = @Quantity,
                          Price = @Price,
                          SupplierID = (SELECT SupplierID FROM Suppliers WHERE Name = @Supplier),
                          Image = @Image,
                          PurchasePrice = @PurchasePrice
                      WHERE ProductID = @ProductID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Name", NameTextBox.Text);
                    command.Parameters.AddWithValue("@Category", CategoryComboBox.SelectedItem.ToString());
                    command.Parameters.AddWithValue("@Quantity", quantity);
                    command.Parameters.AddWithValue("@Price", price);
                    command.Parameters.AddWithValue("@Supplier", SupplierComboBox.SelectedItem.ToString());
                    command.Parameters.AddWithValue("@PurchasePrice", purchasePrice);
                    command.Parameters.AddWithValue("@Image", productImageBytes ?? (object)DBNull.Value);

                    if (editingProduct != null)
                    {
                        command.Parameters.AddWithValue("@ProductID", (int)editingProduct["ProductID"]);
                    }

                    command.ExecuteNonQuery();
                    DialogResult = true;
                }
                catch (SqlException ex)
                {
                    MessageBox.Show($"Помилка збереження: {ex.Message}");
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}