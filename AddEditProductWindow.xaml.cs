using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class AddEditProductWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private DataRowView editingProduct;
        private byte[] productImageBytes;
        private readonly ErrorProvider errorProvider = new ErrorProvider();

        public AddEditProductWindow(DataRowView product = null)
        {
            InitializeComponent();
            editingProduct = product;
            LoadCategoriesAndSuppliers();
            SetupValidation();

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
                    DeleteImageButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void SetupValidation()
        {
            // Валідація назви продукту
            NameTextBox.TextChanged += (s, e) => ValidateName();

            // Валідація кількості
            QuantityTextBox.TextChanged += (s, e) => ValidateQuantity();

            // Валідація ціни
            PriceTextBox.TextChanged += (s, e) => ValidatePrice();

            // Валідація ціни закупівлі
            PurchasePriceTextBox.TextChanged += (s, e) => ValidatePurchasePrice();

            // Валідація вибору категорії
            CategoryComboBox.SelectionChanged += (s, e) => ValidateCategory();

            // Валідація вибору постачальника
            SupplierComboBox.SelectionChanged += (s, e) => ValidateSupplier();

            // Валідація фото при зміні
            BrowseButton.Click += (s, e) => ValidateImage();
            DeleteImageButton.Click += (s, e) => ValidateImage();
        }

        private void ValidateName()
        {
            if (!Validator.ValidateName(NameTextBox.Text))
            {
                errorProvider.SetError("Name", "Назва має містити тільки літери, цифри та пробіли");
                NameTextBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Name");
                NameTextBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
        }

        private void ValidateQuantity()
        {
            if (!Validator.ValidateNumber(QuantityTextBox.Text, false, 0, 10000))
            {
                errorProvider.SetError("Quantity", "Кількість має бути цілим числом від 0 до 10000");
                QuantityTextBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Quantity");
                QuantityTextBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
        }

        private void ValidatePrice()
        {
            if (!Validator.ValidateNumber(PriceTextBox.Text, true, 0.01, 1000000))
            {
                errorProvider.SetError("Price", "Ціна має бути числом від 0.01 до 1 000 000");
                PriceTextBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Price");
                PriceTextBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
        }

        private void ValidatePurchasePrice()
        {
            if (!Validator.ValidateNumber(PurchasePriceTextBox.Text, true, 0.01, 1000000))
            {
                errorProvider.SetError("PurchasePrice", "Ціна закупівлі має бути числом від 0.01 до 1 000 000");
                PurchasePriceTextBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("PurchasePrice");
                PurchasePriceTextBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
        }

        private void ValidateCategory()
        {
            if (CategoryComboBox.SelectedItem == null)
            {
                errorProvider.SetError("Category", "Будь ласка, оберіть категорію");
                CategoryComboBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Category");
                CategoryComboBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
        }

        private void ValidateSupplier()
        {
            if (SupplierComboBox.SelectedItem == null)
            {
                errorProvider.SetError("Supplier", "Будь ласка, оберіть постачальника");
                SupplierComboBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Supplier");
                SupplierComboBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
        }

        private void ValidateImage()
        {
            if (productImageBytes == null || productImageBytes.Length == 0)
            {
                errorProvider.SetError("Image", "Фото є обов'язковим");
                ImageBorder.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Image");
                ImageBorder.BorderBrush = SystemColors.ControlDarkBrush;
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

        private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
        {
            productImageBytes = null;
            ProductImage.Source = null;
            DeleteImageButton.Visibility = Visibility.Collapsed;
            ValidateImage(); // Оновити валідацію
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
                    DeleteImageButton.Visibility = Visibility.Visible;
                    ValidateImage(); // Оновити валідацію
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
            ValidateName();
            ValidateQuantity();
            ValidatePrice();
            ValidatePurchasePrice();
            ValidateCategory();
            ValidateSupplier();
            ValidateImage();

            // Перевірка заповненості всіх обов'язкових полів
            bool hasError =
                !Validator.ValidateName(NameTextBox.Text) ||
                !Validator.ValidateNumber(QuantityTextBox.Text, false, 0, 10000) ||
                !Validator.ValidateNumber(PriceTextBox.Text, true, 0.01, 1000000) ||
                CategoryComboBox.SelectedItem == null ||
                SupplierComboBox.SelectedItem == null ||
                productImageBytes == null || productImageBytes.Length == 0;

            if (hasError)
            {
                MessageBox.Show("Будь ласка, заповніть всі обов'язкові поля та додайте фото.");
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