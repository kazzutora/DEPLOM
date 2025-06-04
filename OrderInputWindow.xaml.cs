using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net;
using System.Net.Mail;
using System.IO;

namespace WpfApp1
{
    public partial class OrderInputWindow : Window
    {
        public int QuantityOrdered { get; private set; }
        public int? SelectedSupplierId { get; private set; }
        public DateTime? ExpectedDate { get; private set; }
        private DataTable suppliersData;
        private readonly int currentQuantity;
        private readonly int minQuantity;
        private readonly string productName;
        private readonly decimal purchasePrice;
        private readonly string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";

        public OrderInputWindow(string productName, decimal purchasePrice,
                              int currentQuantity, int minQuantity,
                              int currentSupplierId, string currentSupplierName)
        {
            InitializeComponent();
            this.currentQuantity = currentQuantity;
            this.minQuantity = minQuantity;
            this.productName = productName;
            this.purchasePrice = purchasePrice;

            LoadSuppliers();
            InitializeFields(productName, purchasePrice, currentQuantity, minQuantity,
                           currentSupplierId, currentSupplierName);
        }

        private void LoadSuppliers()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var adapter = new SqlDataAdapter("SELECT SupplierID, Name, Email FROM Suppliers", connection);
                    suppliersData = new DataTable();
                    adapter.Fill(suppliersData);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження постачальників: {ex.Message}",
                              "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeFields(string productName, decimal purchasePrice,
                                    int currentQuantity, int minQuantity,
                                    int currentSupplierId, string currentSupplierName)
        {
            ProductNameTextBlock.Text = productName;
            PurchasePriceTextBlock.Text = purchasePrice.ToString("N2") + " грн";
            CurrentQuantityTextBlock.Text = currentQuantity.ToString();
            MinQuantityTextBlock.Text = minQuantity.ToString();

            int recommended = Math.Max(0, minQuantity - currentQuantity);
            RecommendedQuantityTextBlock.Text = recommended.ToString();
            QuantityTextBox.Text = recommended.ToString();

            SupplierComboBox.ItemsSource = suppliersData?.DefaultView;
            SupplierComboBox.DisplayMemberPath = "Name";
            SupplierComboBox.SelectedValuePath = "SupplierID";

            if (currentSupplierId > 0)
            {
                foreach (DataRowView item in SupplierComboBox.Items)
                {
                    if (Convert.ToInt32(item["SupplierID"]) == currentSupplierId)
                    {
                        SupplierComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(currentSupplierName))
            {
                SupplierComboBox.Text = currentSupplierName;
            }

            ExpectedDatePicker.SelectedDate = DateTime.Now.AddDays(3);
            CalculateTotal();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            if (int.TryParse(QuantityTextBox.Text, out int quantity) &&
                decimal.TryParse(PurchasePriceTextBlock.Text.Replace(" грн", ""), out decimal price))
            {
                TotalAmountTextBlock.Text = (price * quantity).ToString("N2") + " грн";
            }
            else
            {
                TotalAmountTextBlock.Text = "0.00 грн";
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Будь ласка, введіть коректну додатню кількість товару.",
                              "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SupplierComboBox.SelectedItem == null && !string.IsNullOrEmpty(SupplierComboBox.Text))
            {
                MessageBox.Show("Будь ласка, оберіть постачальника зі списку.",
                              "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ExpectedDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Будь ласка, вкажіть очікувану дату поставки.",
                              "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            QuantityOrdered = quantity;
            SelectedSupplierId = SupplierComboBox.SelectedItem != null ?
                Convert.ToInt32(((DataRowView)SupplierComboBox.SelectedItem)["SupplierID"]) :
                (int?)null;
            ExpectedDate = ExpectedDatePicker.SelectedDate;

            // Відправка email постачальнику
            if (SupplierComboBox.SelectedItem != null)
            {
                var selectedSupplier = (DataRowView)SupplierComboBox.SelectedItem;
                string supplierEmail = selectedSupplier["Email"]?.ToString();
                string supplierName = selectedSupplier["Name"].ToString();

                if (!string.IsNullOrEmpty(supplierEmail))
                {
                    SendOrderEmail(supplierEmail, supplierName);
                }
                else
                {
                    MessageBox.Show("Не вказано email постачальника. Замовлення створено, але email не відправлено.",
                                  "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            DialogResult = true;
            Close();
        }

        private void SendOrderEmail(string supplierEmail, string supplierName)
        {
            string tempPdfPath = null;
            try
            {
                // Генеруємо тимчасовий PDF-файл
                tempPdfPath = Path.Combine(Path.GetTempPath(), $"Order_{Guid.NewGuid()}.pdf");
                OrderPdfGenerator.GenerateOrderEmailPdf
                    (
                    tempPdfPath,
                    productName,
                    QuantityOrdered,
                    purchasePrice,
                    supplierName,
                    DateTime.Now,
                    ExpectedDate.Value
                );

                // Налаштування SMTP (замініть на свої)
                using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.Credentials = new NetworkCredential("diabloroale9@gmail.com", "mlfb nkqk mdfa qjjr");
                    client.EnableSsl = true;

                    // Додано перевірку підключення
                    try
                    {
                        // Тестуємо підключення
                        var timeout = 5000; // 5 секунд
                        var task = Task.Run(() => client.Send(new MailMessage()));
                        if (Task.WaitAny(new[] { task }, timeout) == -1)
                        {
                            throw new TimeoutException("Час очікування підключення до SMTP-сервера вийшов");
                        }
                    }
                    catch (Exception testEx)
                    {
                        throw new Exception($"Не вдалося підключитися до SMTP-сервера: {testEx.Message}", testEx);
                    }

                    // Створення повідомлення
                    using (MailMessage message = new MailMessage())
                    {
                        message.From = new MailAddress("your_email@gmail.com");
                        message.Subject = $"Нове замовлення - {productName}";
                        message.Body = $"Шановний постачальник,\n\nДодано нове замовлення. Деталі у додатку.\n\nТовар: {productName}\nКількість: {QuantityOrdered}\nОчікувана дата поставки: {ExpectedDate.Value:dd.MM.yyyy}\n\nЗ повагою,\nВаш магазин";
                        message.IsBodyHtml = false;
                        message.To.Add(supplierEmail);
                        message.Attachments.Add(new Attachment(tempPdfPath));

                        // Відправка
                        client.Send(message);
                    }

                    MessageBox.Show($"Замовлення відправлено постачальнику {supplierName} на email {supplierEmail}", "Успіх");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка відправки email: {ex.Message}\nЗамовлення створено, але email не вдалося відправити.",
                              "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Видаляємо тимчасовий файл
                if (tempPdfPath != null && File.Exists(tempPdfPath))
                {
                    try
                    {
                        File.Delete(tempPdfPath);
                    }
                    catch { }
                }
            }
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}