using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class OrderInputWindow : Window
    {
        public int QuantityOrdered { get; private set; }
        public int? SelectedSupplierId { get; private set; }
        public DateTime? ExpectedDate { get; private set; }
        private DataTable suppliersData;

        public OrderInputWindow(string productName, decimal purchasePrice,
                                int currentQuantity, int minQuantity,
                                int currentSupplierId, string currentSupplierName)
        {
            InitializeComponent();
            LoadSuppliers();
            InitializeFields(productName, purchasePrice, currentQuantity, minQuantity, currentSupplierId, currentSupplierName);
        }

        private void LoadSuppliers()
        {
            string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT SupplierID, Name FROM Suppliers";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    suppliersData = new DataTable();
                    adapter.Fill(suppliersData);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження постачальників: {ex.Message}", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeFields(string productName, decimal purchasePrice,
                                      int currentQuantity, int minQuantity,
                                      int currentSupplierId, string currentSupplierName)
        {
            // Ініціалізація текстових полів
            ProductNameTextBlock.Text = productName;
            PurchasePriceTextBlock.Text = purchasePrice.ToString("N2") + " грн";
            CurrentQuantityTextBlock.Text = currentQuantity.ToString();
            MinQuantityTextBlock.Text = minQuantity.ToString();

            // Розрахунок рекомендованої кількості
            int recommended = Math.Max(0, minQuantity - currentQuantity);
            RecommendedQuantityTextBlock.Text = recommended.ToString();
            QuantityTextBox.Text = recommended.ToString();

            // Налаштування комбобоксу постачальників
            SupplierComboBox.ItemsSource = suppliersData?.DefaultView;
            SupplierComboBox.DisplayMemberPath = "Name";
            SupplierComboBox.SelectedValuePath = "SupplierID";

            // Встановлення поточного постачальника
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

            // Встановлення дати за замовчуванням
            ExpectedDatePicker.SelectedDate = DateTime.Now.AddDays(3);

            CalculateTotal();
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
                decimal total = price * quantity;
                TotalAmountTextBlock.Text = total.ToString("N2") + " грн";
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
                MessageBox.Show("Будь ласка, введіть коректну кількість товару.", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Перевірка вибору постачальника
            if (SupplierComboBox.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)SupplierComboBox.SelectedItem;
                SelectedSupplierId = Convert.ToInt32(selectedRow["SupplierID"]);
            }
            else if (!string.IsNullOrEmpty(SupplierComboBox.Text))
            {
                MessageBox.Show("Будь ласка, оберіть постачальника зі списку.", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Перевірка дати поставки
            if (!ExpectedDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Будь ласка, вкажіть очікувану дату поставки.", "Попередження",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ExpectedDate = ExpectedDatePicker.SelectedDate;

            QuantityOrdered = quantity;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}