﻿using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using static WpfApp1.BasePdfPageEventHandler;
using System.Windows.Media;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;



namespace WpfApp1
{
    public partial class OrderManagementWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private FinancialReportData currentReportData;
        private DataTable productsData;
        private DataTable lowStockProductsData;
        private DataTable ordersData;

        public OrderManagementWindow()
        {
            
            InitializeComponent();
            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Today;
            LoadData();
            LoadOrders();
            SetupContextMenu();
        }

        private void LoadData()
        {
            try
            {
                // Завантаження всіх товарів з інформацією про постачальника
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                    SELECT p.ProductID, p.Name, p.Quantity, p.Price, p.MinQuantity, p.PurchasePrice, 
                           c.CategoryName, s.Name AS SupplierName, s.SupplierID,
                           CASE WHEN p.Quantity <= p.MinQuantity THEN 'Потрібно замовлення'
                                ELSE 'Достатньо' END AS OrderStatus
                    FROM Products p
                    JOIN Categories c ON p.CategoryID = c.CategoryID
                    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                    ORDER BY p.Name";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    productsData = new DataTable();
                    adapter.Fill(productsData);

                    AllProductsDataGrid.ItemsSource = productsData.DefaultView;
                }

                // Завантаження товарів, що потребують замовлення
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
               SELECT p.ProductID, p.Name, p.Quantity, p.MinQuantity, p.PurchasePrice, 
           c.CategoryName, s.Name AS SupplierName, s.SupplierID, s.Email AS SupplierEmail,
           (p.MinQuantity - p.Quantity) AS OrderQuantity
    FROM Products p
    JOIN Categories c ON p.CategoryID = c.CategoryID
    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
    WHERE p.Quantity < p.MinQuantity
    ORDER BY p.Quantity ASC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    lowStockProductsData = new DataTable();
                    adapter.Fill(lowStockProductsData);

                    LowStockDataGrid.ItemsSource = lowStockProductsData.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження даних: {ex.Message}", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrders()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                    SELECT o.OrderID, p.Name AS ProductName, o.QuantityOrdered, o.PurchasePrice, 
                           (o.QuantityOrdered * o.PurchasePrice) AS TotalAmount, 
                           o.OrderDate, o.ExpectedDeliveryDate, o.Status, s.Name AS SupplierName
                    FROM Orders o
                    JOIN Products p ON o.ProductID = p.ProductID
                    LEFT JOIN Suppliers s ON o.SupplierID = s.SupplierID
                    ORDER BY o.OrderDate DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    ordersData = new DataTable();
                    adapter.Fill(ordersData);

                    OrdersDataGrid.ItemsSource = ordersData.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження замовлень: {ex.Message}", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isSelected = OrdersDataGrid.SelectedItem != null;
            ConfirmOrderBtn.IsEnabled = isSelected;
            CancelOrderBtn.IsEnabled = isSelected;
            DeleteOrderBtn.IsEnabled = isSelected;
        }
        private void DeleteOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem == null) return;

            DataRowView row = (DataRowView)OrdersDataGrid.SelectedItem;
            int orderId = Convert.ToInt32(row["OrderID"]);
            string status = row["Status"].ToString();

            // Заборона видаляти підтверджені замовлення
            if (status == "Підтверджено")
            {
                MessageBox.Show("Не можна видаляти підтверджені замовлення!",
                                "Помилка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Видалити замовлення #{orderId}? Цю дію неможливо скасувати.",
                                "Підтвердження видалення",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                DeleteOrder(orderId);
            }
        }

        private void DeleteOrder(int orderId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "DELETE FROM Orders WHERE OrderID = @OrderID",
                        connection);

                    cmd.Parameters.AddWithValue("@OrderID", orderId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Замовлення #{orderId} видалено!", "Успіх");
                        LoadOrders(); // Оновлюємо список
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка видалення: {ex.Message}", "Помилка");
            }
        }
        private void ConfirmOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem == null) return;

            DataRowView row = (DataRowView)OrdersDataGrid.SelectedItem;
            int orderId = Convert.ToInt32(row["OrderID"]);
            int productId = GetProductIdByOrderId(orderId);
            int quantity = Convert.ToInt32(row["QuantityOrdered"]);

            if (MessageBox.Show($"Підтвердити замовлення {orderId}?\nТовар буде додано на склад.",
                               "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ConfirmOrder(orderId, productId, quantity);
            }
        }

        private void CancelOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem == null) return;

            DataRowView row = (DataRowView)OrdersDataGrid.SelectedItem;
            int orderId = Convert.ToInt32(row["OrderID"]);

            if (MessageBox.Show($"Скасувати замовлення {orderId}?",
                               "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                CancelOrder(orderId);
            }
        }

        private int GetProductIdByOrderId(int orderId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "SELECT ProductID FROM Orders WHERE OrderID = @OrderID",
                        connection);

                    cmd.Parameters.AddWithValue("@OrderID", orderId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка");
                return 0;
            }
        }

        private void ConfirmOrder(int orderId, int productId, int quantity)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // Видаляємо перевірку доступності - вона тут не потрібна
                        // Оновлюємо статус замовлення
                        SqlCommand updateOrderCmd = new SqlCommand(
                            "UPDATE Orders SET Status = 'Підтверджено' WHERE OrderID = @OrderID",
                            connection, transaction);
                        updateOrderCmd.Parameters.AddWithValue("@OrderID", orderId);
                        updateOrderCmd.ExecuteNonQuery();

                        // Збільшуємо кількість товару
                        SqlCommand updateProductCmd = new SqlCommand(
                            "UPDATE Products SET Quantity = Quantity + @Quantity WHERE ProductID = @ProductID",
                            connection, transaction);
                        updateProductCmd.Parameters.AddWithValue("@Quantity", quantity);
                        updateProductCmd.Parameters.AddWithValue("@ProductID", productId);
                        updateProductCmd.ExecuteNonQuery();

                        transaction.Commit();
                        MessageBox.Show($"Замовлення {orderId} підтверджено!\nТовар додано на склад.", "Успіх");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Помилка підтвердження замовлення: {ex.Message}", "Помилка");
                        throw;
                    }
                }

                LoadData();      // Оновлюємо дані товарів
                LoadOrders();   // Оновлюємо список замовлень
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка підтвердження замовлення: {ex.Message}", "Помилка");
            }
        }
        private int GetAvailableQuantity(int productId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "SELECT Quantity FROM Products WHERE ProductID = @ProductID",
                        connection);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch
            {
                return 0; // У разі помилки повертаємо 0
            }
        }

        private void CancelOrder(int orderId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE Orders SET Status = 'Скасовано' WHERE OrderID = @OrderID",
                        connection);

                    cmd.Parameters.AddWithValue("@OrderID", orderId);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show($"Замовлення {orderId} скасовано!", "Успіх");
                    LoadOrders(); // Оновлюємо список замовлень
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка скасування замовлення: {ex.Message}", "Помилка");
            }
        }

        private void RefreshOrdersBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyOrdersFilter();
        }

        private void SearchOrderTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyOrdersFilter();
        }

        private void ApplyOrdersFilter()
        {
            
            if (ordersData == null) return;

            string statusFilter = "";
            if (StatusFilterComboBox.SelectedIndex > 0 &&
                StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string status = selectedItem.Content.ToString();
                if (ordersData.Columns.Contains("Status"))
                    statusFilter = $"Status = '{status}'";
            }


            string searchText = SearchOrderTextBox.Text.Replace("'", "''");
            string searchFilter = string.IsNullOrEmpty(searchText)
                ? ""
                : $"ProductName LIKE '%{searchText}%' OR SupplierName LIKE '%{searchText}%'";

            string finalFilter = "";
            if (!string.IsNullOrEmpty(statusFilter)) finalFilter += statusFilter;
            if (!string.IsNullOrEmpty(searchFilter))
            {
                if (!string.IsNullOrEmpty(finalFilter)) finalFilter += " AND ";
                finalFilter += searchFilter;
            }

            ordersData.DefaultView.RowFilter = finalFilter;
        }

        private void OrderProductBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AllProductsDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, оберіть товар для замовлення.", "Увага",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView row = (DataRowView)AllProductsDataGrid.SelectedItem;
            ShowOrderDialog(row);
        }

        private void OrderLowStockBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LowStockDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, оберіть товар для замовлення.", "Увага",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView row = (DataRowView)LowStockDataGrid.SelectedItem;
            ShowOrderDialog(row);
        }

        private void ShowOrderDialog(DataRowView row)
        {
            try
            {
                int productId = Convert.ToInt32(row["ProductID"]);
                string productName = row["Name"].ToString();
                decimal purchasePrice = Convert.ToDecimal(row["PurchasePrice"]);
                int currentQuantity = Convert.ToInt32(row["Quantity"]);
                int minQuantity = Convert.ToInt32(row["MinQuantity"]);

                int supplierId = 0;
                string supplierName = "";

                if (row.Row.Table.Columns.Contains("SupplierID") && row["SupplierID"] != DBNull.Value)
                {
                    supplierId = Convert.ToInt32(row["SupplierID"]);
                }
                if (row.Row.Table.Columns.Contains("SupplierName") && row["SupplierName"] != DBNull.Value)
                {
                    supplierName = row["SupplierName"].ToString();
                }

                // Оновлено: передаємо productId першим параметром
                var orderDialog = new OrderInputWindow(
                    productId,
                    productName,
                    purchasePrice,
                    currentQuantity,
                    minQuantity,
                    supplierId,
                    supplierName);

                if (orderDialog.ShowDialog() == true)
                {
                    int quantityOrdered = orderDialog.QuantityOrdered;
                    int? selectedSupplierId = orderDialog.SelectedSupplierId;
                    DateTime? expectedDate = orderDialog.ExpectedDate;

                    CreateOrder(productId, quantityOrdered, purchasePrice, selectedSupplierId, expectedDate);

                    if (selectedSupplierId.HasValue)
                    {
                        UpdateProductSupplier(productId, selectedSupplierId.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відкритті вікна замовлення: {ex.Message}", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProductSupplier(int productId, int supplierId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE Products SET SupplierID = @SupplierID WHERE ProductID = @ProductID",
                        connection);

                    cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при оновленні постачальника: {ex.Message}", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateOrder(int productId, int quantityOrdered, decimal purchasePrice,
                                 int? supplierId, DateTime? expectedDate)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO Orders (ProductID, QuantityOrdered, OrderDate, Status, PurchasePrice, SupplierID, ExpectedDeliveryDate) " +
                        "VALUES (@ProductID, @QuantityOrdered, GETDATE(), 'Нове', @PurchasePrice, @SupplierID, @ExpectedDeliveryDate)",
                        connection);

                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.Parameters.AddWithValue("@QuantityOrdered", quantityOrdered);
                    cmd.Parameters.AddWithValue("@PurchasePrice", purchasePrice);
                    cmd.Parameters.AddWithValue("@SupplierID", (object)supplierId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ExpectedDeliveryDate", (object)expectedDate ?? DBNull.Value);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show($"Замовлення на {quantityOrdered} од. товару створено успішно!\n" +
                                  $"Очікувана дата поставки: {expectedDate?.ToShortDateString() ?? "не вказано"}",
                                  "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadOrders();
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при створенні замовлення: {ex.Message}", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchAllTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (productsData != null)
            {
                string searchText = SearchAllTextBox.Text.Replace("'", "''");
                productsData.DefaultView.RowFilter = $"Name LIKE '%{searchText}%' OR SupplierName LIKE '%{searchText}%'";
            }
        }

        private void SearchLowStockTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lowStockProductsData != null)
            {
                string searchText = SearchLowStockTextBox.Text.Replace("'", "''");
                lowStockProductsData.DefaultView.RowFilter = $"Name LIKE '%{searchText}%' OR SupplierName LIKE '%{searchText}%'";
            }
        }

        private void AutoOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (lowStockProductsData == null || lowStockProductsData.Rows.Count == 0)
            {
                MessageBox.Show("Немає товарів, що потребують замовлення.", "Інформація",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Структура для зберігання інформації про товар
                var orderItems = new List<AutoOrderItem>();

                // Збираємо інформацію про товари, що потребують замовлення
                foreach (DataRow row in lowStockProductsData.Rows)
                {
                    int productId = Convert.ToInt32(row["ProductID"]);
                    int currentQuantity = Convert.ToInt32(row["Quantity"]);
                    int minQuantity = Convert.ToInt32(row["MinQuantity"]);
                    int orderQuantity = minQuantity - currentQuantity;

                    if (orderQuantity <= 0) continue;

                    int? supplierId = row["SupplierID"] != DBNull.Value
                        ? Convert.ToInt32(row["SupplierID"])
                        : (int?)null;

                    string supplierName = row["SupplierName"]?.ToString() ?? "Без постачальника";
                    string supplierEmail = row.Table.Columns.Contains("SupplierEmail")
                        ? row["SupplierEmail"]?.ToString()
                        : null;

                    orderItems.Add(new AutoOrderItem
                    {
                        ProductId = productId,
                        ProductName = row["Name"].ToString(),
                        Quantity = orderQuantity,
                        PurchasePrice = Convert.ToDecimal(row["PurchasePrice"]),
                        SupplierId = supplierId,
                        SupplierName = supplierName,
                        SupplierEmail = supplierEmail
                    });
                }

                // Групуємо товари за постачальниками
                var groupedOrders = orderItems
                    .GroupBy(item => item.SupplierId)
                    .ToList();

                int totalOrdersCreated = 0;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Обробляємо кожну групу постачальника
                    foreach (var group in groupedOrders)
                    {
                        var supplierId = group.Key;
                        var firstItem = group.First();

                        // Відправляємо email тільки якщо є пошта постачальника
                        if (!string.IsNullOrEmpty(firstItem.SupplierEmail))
                        {
                            try
                            {
                                // Формуємо список товарів для цього постачальника
                                var itemsForEmail = group.Select(item => new
                                {
                                    item.ProductName,
                                    item.Quantity,
                                    item.PurchasePrice,
                                    Total = item.Quantity * item.PurchasePrice
                                }).ToList();

                                // Відправляємо згрупований email
                                SendGroupedOrderEmail(
                                    firstItem.SupplierEmail,
                                    firstItem.SupplierName,
                                    itemsForEmail,
                                    DateTime.Now.AddDays(7)
                                );
                            }
                            catch (Exception emailEx)
                            {
                                MessageBox.Show($"Не вдалося відправити email постачальнику {firstItem.SupplierName}: {emailEx.Message}",
                                              "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        // Створюємо замовлення для кожного товару
                        foreach (var item in group)
                        {
                            DateTime expectedDate = DateTime.Now.AddDays(7);

                            SqlCommand cmd = new SqlCommand(
                                @"INSERT INTO Orders 
                          (ProductID, QuantityOrdered, OrderDate, Status, 
                           PurchasePrice, SupplierID, ExpectedDeliveryDate) 
                          VALUES 
                          (@ProductID, @QuantityOrdered, GETDATE(), 'Нове', 
                           @PurchasePrice, @SupplierID, @ExpectedDeliveryDate)",
                                connection);

                            cmd.Parameters.AddWithValue("@ProductID", item.ProductId);
                            cmd.Parameters.AddWithValue("@QuantityOrdered", item.Quantity);
                            cmd.Parameters.AddWithValue("@PurchasePrice", item.PurchasePrice);
                            cmd.Parameters.AddWithValue("@SupplierID", (object)item.SupplierId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ExpectedDeliveryDate", expectedDate);

                            cmd.ExecuteNonQuery();
                            totalOrdersCreated++;
                        }
                    }
                }

                MessageBox.Show($"Створено {totalOrdersCreated} автоматичних замовлень!\n" +
                              $"Відправлено email постачальникам.",
                              "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

                // Оновлюємо дані
                LoadData();
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при автоматичному замовленні: {ex.Message}", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private class AutoOrderItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal PurchasePrice { get; set; }
            public int? SupplierId { get; set; }
            public string SupplierName { get; set; }
            public string SupplierEmail { get; set; }
        }

        // Метод для відправки згрупованого email
        private void SendGroupedOrderEmail(string supplierEmail, string supplierName,
                                          IEnumerable<dynamic> items, DateTime expectedDate)
        {
            try
            {
                using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.Credentials = new NetworkCredential("diabloroale9@gmail.com", "mlfb nkqk mdfa qjjr"); 
                    client.EnableSsl = true;

                    using (MailMessage message = new MailMessage())
                    {
                        message.From = new MailAddress("your_email@gmail.com");
                        message.Subject = $"Замовлення товарів від {DateTime.Today:dd.MM.yyyy}";

                        // Формуємо HTML-тіло листа
                        string htmlBody = $@"
                    <html>
                    <body>
                        <h2>Шановний постачальник {supplierName},</h2>
                        <p>Ми сформували для Вас замовлення. Деталі:</p>
                        <table border='1' cellpadding='5' cellspacing='0'>
                            <tr>
                                <th>Товар</th>
                                <th>Кількість</th>
                                <th>Ціна за одиницю</th>
                                <th>Сума</th>
                            </tr>";

                        decimal totalAmount = 0;

                        foreach (var item in items)
                        {
                            htmlBody += $@"
                        <tr>
                            <td>{item.ProductName}</td>
                            <td>{item.Quantity}</td>
                            <td>{item.PurchasePrice:N2} грн</td>
                            <td>{item.Total:N2} грн</td>
                        </tr>";
                            totalAmount += item.Total;
                        }

                        htmlBody += $@"
                            <tr>
                                <td colspan='3'><strong>Загальна сума</strong></td>
                                <td><strong>{totalAmount:N2} грн</strong></td>
                            </tr>
                        </table>
                        <p><strong>Очікувана дата поставки:</strong> {expectedDate:dd.MM.yyyy}</p>
                        <p>З повагою,<br/>Ваш магазин</p>
                    </body>
                    </html>";

                        message.Body = htmlBody;
                        message.IsBodyHtml = true;
                        message.To.Add(supplierEmail);

                        client.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка відправки email: {ex.Message}", ex);
            }
        }
        private void SetupContextMenu()
        {
            // Для таблиці всіх товарів
            ContextMenu menuAllProducts = new ContextMenu();
            MenuItem editMinItem = new MenuItem { Header = "Редагувати мінімальну кількість" };
            editMinItem.Click += EditMinQuantity_Click;
            menuAllProducts.Items.Add(editMinItem);
            AllProductsDataGrid.ContextMenu = menuAllProducts;

            // Для таблиці товарів з низьким запасом
            ContextMenu menuLowStock = new ContextMenu();
            MenuItem editMinItem2 = new MenuItem { Header = "Редагувати мінімальну кількість" };
            editMinItem2.Click += EditMinQuantity_Click;
            menuLowStock.Items.Add(editMinItem2);
            LowStockDataGrid.ContextMenu = menuLowStock;
        }
        private void EditMinQuantity_Click(object sender, RoutedEventArgs e)
        {
            DataGrid targetGrid = AllProductsDataGrid.ContextMenu.IsOpen ? AllProductsDataGrid :
                                 LowStockDataGrid.ContextMenu.IsOpen ? LowStockDataGrid : null;

            if (targetGrid?.SelectedItem == null) return;

            DataRowView row = (DataRowView)targetGrid.SelectedItem;
            int productId = Convert.ToInt32(row["ProductID"]);
            string productName = row["Name"].ToString();
            int currentMin = Convert.ToInt32(row["MinQuantity"]);

            // Показуємо вікно редагування
            var editDialog = new EditMinQuantityWindow(productName, currentMin);
            if (editDialog.ShowDialog() == true)
            {
                UpdateMinQuantity(productId, editDialog.NewMinQuantity);
            }
        }

        private void UpdateMinQuantity(int productId, int newMinQuantity)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE Products SET MinQuantity = @MinQuantity WHERE ProductID = @ProductID",
                        connection);
                    cmd.Parameters.AddWithValue("@MinQuantity", newMinQuantity);
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.ExecuteNonQuery();
                }
                LoadData(); // Оновлюємо дані
                MessageBox.Show("Мінімальна кількість успішно оновлена!", "Успіх");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення: {ex.Message}", "Помилка");
            }
        }
        private void EditMinQuantityBtn_Click(object sender, RoutedEventArgs e) // Перейменовано
        {
            DataGrid targetGrid = null;
            if (AllProductsDataGrid.IsVisible)
                targetGrid = AllProductsDataGrid;
            else if (LowStockDataGrid.IsVisible)
                targetGrid = LowStockDataGrid;

            if (targetGrid?.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, оберіть товар для редагування",
                               "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView row = (DataRowView)targetGrid.SelectedItem;
            EditMinQuantity(row); // Виклик методу редагування
        }


        // ДОДАНО МЕТОД РЕДАГУВАННЯ
        private void EditMinQuantity(DataRowView row)
        {
            try
            {
                int productId = Convert.ToInt32(row["ProductID"]);
                string productName = row["Name"].ToString();
                int currentMin = Convert.ToInt32(row["MinQuantity"]);

                // Викликаємо вікно редагування
                var editDialog = new EditMinQuantityWindow(productName, currentMin);
                if (editDialog.ShowDialog() == true)
                {
                    UpdateMinQuantity(productId, editDialog.NewMinQuantity);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при редагуванні: {ex.Message}", "Помилка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Оберіть період для формування звіту");
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
            DateTime endDate = EndDatePicker.SelectedDate.Value.Date.AddDays(1);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // 1. ЗАПИТ ДЛЯ ПРОДАЖІВ
                    string salesQuery = @"
                        SELECT 
                            s.SaleDate,
                            p.Name AS ProductName,
                            s.QuantitySold,
                            p.Price AS SalePrice,
                            p.PurchasePrice,
                            (p.Price - p.PurchasePrice) * s.QuantitySold AS Profit
                        FROM Sales s
                        JOIN Products p ON s.ProductID = p.ProductID
                        WHERE s.SaleDate BETWEEN @StartDate AND @EndDate
                        ORDER BY s.SaleDate DESC";

                    SqlDataAdapter salesAdapter = new SqlDataAdapter(salesQuery, connection);
                    salesAdapter.SelectCommand.Parameters.AddWithValue("@StartDate", startDate);
                    salesAdapter.SelectCommand.Parameters.AddWithValue("@EndDate", endDate);

                    DataTable salesData = new DataTable();
                    salesAdapter.Fill(salesData);

                    // 2. ЗАПИТ ДЛЯ ВИТРАТ НА ЗАКУПІВЛЮ
                    string expensesQuery = @"
                        SELECT 
                            o.OrderDate,
                            p.Name AS ProductName,
                            o.QuantityOrdered,
                            o.PurchasePrice,
                            (o.QuantityOrdered * o.PurchasePrice) AS TotalExpense
                        FROM Orders o
                        JOIN Products p ON o.ProductID = p.ProductID
                        WHERE o.Status = 'Підтверджено'
                            AND o.OrderDate BETWEEN @StartDate AND @EndDate";

                    SqlDataAdapter expensesAdapter = new SqlDataAdapter(expensesQuery, connection);
                    expensesAdapter.SelectCommand.Parameters.AddWithValue("@StartDate", startDate);
                    expensesAdapter.SelectCommand.Parameters.AddWithValue("@EndDate", endDate);

                    DataTable expensesData = new DataTable();
                    expensesAdapter.Fill(expensesData);

                    // 3. РОЗРАХУНКИ
                    decimal totalRevenue = 0;
                    decimal totalExpenses = 0;
                    int salesCount = 0;

                    // Продажі
                    foreach (DataRow row in salesData.Rows)
                    {
                        decimal salePrice = Convert.ToDecimal(row["SalePrice"]);
                        int quantity = Convert.ToInt32(row["QuantitySold"]);
                        totalRevenue += salePrice * quantity;
                        salesCount += quantity;
                    }

                    // Витрати (замовлення)
                    foreach (DataRow row in expensesData.Rows)
                    {
                        decimal expense = Convert.ToDecimal(row["TotalExpense"]);
                        totalExpenses += expense;
                    }

                    // 4. ОНОВЛЕННЯ ІНТЕРФЕЙСУ
                    ReportDataGrid.ItemsSource = salesData.DefaultView;

                    TotalRevenueText.Text = totalRevenue.ToString("N2") + " грн";
                    TotalExpensesText.Text = totalExpenses.ToString("N2") + " грн";

                    decimal netProfit = totalRevenue - totalExpenses;
                    NetProfitText.Text = netProfit.ToString("N2") + " грн";
                    SalesCountText.Text = salesCount.ToString();

                    NetProfitText.Foreground = netProfit >= 0 ? Brushes.Green : Brushes.Red;

                    // 5. ЗБЕРЕЖЕННЯ ДАНИХ ДЛЯ ЕКСПОРТУ
                    currentReportData = new FinancialReportData
                    {
                        StartDate = startDate,
                        EndDate = endDate,
                        SalesData = salesData,
                        ExpensesData = expensesData,
                        TotalRevenue = totalRevenue,
                        TotalExpenses = totalExpenses,
                        NetProfit = netProfit,
                        SalesCount = salesCount
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка генерації звіту: {ex.Message}", "Помилка");
                ClearReportData();
            }
        }

        private void ClearReportData()
        {
            ReportDataGrid.ItemsSource = null;
            TotalRevenueText.Text = "0.00 грн";
            TotalExpensesText.Text = "0.00 грн";
            NetProfitText.Text = "0.00 грн";
            SalesCountText.Text = "0";
            NetProfitText.Foreground = Brushes.Black;
        }
        // ДОДАНО МЕТОД ОНОВЛЕННЯ В БД
        private void ExportPdfBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData == null)
            {
                MessageBox.Show("Спочатку згенеруйте звіт", "Помилка");
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"FinancialReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                GeneratePdfReport(saveDialog.FileName, currentReportData);
            }
        }

        private void GeneratePdfReport(string filePath, FinancialReportData reportData)
        {
            try
            {
                // Створення документа
                Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                document.Open();

                // Шрифти з підтримкою кирилиці
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font titleFont = new Font(baseFont, 18, Font.BOLD);
                Font headerFont = new Font(baseFont, 14, Font.BOLD);
                Font normalFont = new Font(baseFont, 12);
                Font boldFont = new Font(baseFont, 12, Font.BOLD);

                // Заголовок
                Paragraph title = new Paragraph("ФІНАНСОВИЙ ЗВІТ", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20;
                document.Add(title);

                // Період звіту
                document.Add(new Paragraph($"Період: з {reportData.StartDate:dd.MM.yyyy} по {reportData.EndDate.AddDays(-1):dd.MM.yyyy}", normalFont));
                document.Add(Chunk.NEWLINE);

                // Статистика
                Paragraph statsHeader = new Paragraph("ЗАГАЛЬНА СТАТИСТИКА", headerFont);
                statsHeader.SpacingAfter = 10; // Додаємо відступ після заголовка
                document.Add(statsHeader);

                document.Add(new Paragraph($"Загальний дохід: {reportData.TotalRevenue:N2} грн", normalFont));
                document.Add(new Paragraph($"Витрати на закупівлю: {reportData.TotalExpenses:N2} грн", normalFont));

                // Чистий прибуток з кольором
                Paragraph netProfitParagraph = new Paragraph(
                    $"Чистий прибуток: {reportData.NetProfit:N2} грн",
                    reportData.NetProfit >= 0
                        ? new Font(baseFont, 12, Font.BOLD, BaseColor.GREEN)
                        : new Font(baseFont, 12, Font.BOLD, BaseColor.RED)
                );
                document.Add(netProfitParagraph);

                document.Add(new Paragraph($"Кількість продажів: {reportData.SalesCount}", normalFont));
                document.Add(Chunk.NEWLINE);

                // Таблиця продажів
                Paragraph salesHeader = new Paragraph("ДЕТАЛІЗАЦІЯ ПРОДАЖІВ", headerFont);
                salesHeader.SpacingBefore = 15; // Відступ перед заголовком таблиці
                salesHeader.SpacingAfter = 8;   // Відступ після заголовка таблиці
                document.Add(salesHeader);
                PdfPTable salesTable = new PdfPTable(6);
                salesTable.WidthPercentage = 100;
                salesTable.SetWidths(new float[] { 1.5f, 3f, 1.5f, 2f, 2f, 2f });

                // Заголовки стовпців
                AddCell(salesTable, "Дата", boldFont);
                AddCell(salesTable, "Товар", boldFont);
                AddCell(salesTable, "Кількість", boldFont);
                AddCell(salesTable, "Ціна продажу", boldFont);
                AddCell(salesTable, "Собівартість", boldFont);
                AddCell(salesTable, "Прибуток", boldFont);

                // Дані продажів
                decimal totalProfit = 0;
                foreach (DataRow row in reportData.SalesData.Rows)
                {
                    AddCell(salesTable, Convert.ToDateTime(row["SaleDate"]).ToString("dd.MM.yyyy"), normalFont);
                    AddCell(salesTable, row["ProductName"].ToString(), normalFont);
                    AddCell(salesTable, row["QuantitySold"].ToString(), normalFont);
                    AddCell(salesTable, Convert.ToDecimal(row["SalePrice"]).ToString("N2") + " грн", normalFont);
                    AddCell(salesTable, Convert.ToDecimal(row["PurchasePrice"]).ToString("N2") + " грн", normalFont);

                    decimal profit = Convert.ToDecimal(row["Profit"]);
                    totalProfit += profit;
                    AddCell(salesTable, profit.ToString("N2") + " грн", normalFont);
                }

                // Підсумковий рядок
                AddCell(salesTable, "РАЗОМ:", boldFont);
                AddCell(salesTable, "", boldFont);
                AddCell(salesTable, "", boldFont);
                AddCell(salesTable, "", boldFont);
                AddCell(salesTable, "", boldFont);
                AddCell(salesTable, totalProfit.ToString("N2") + " грн", boldFont);

                document.Add(salesTable);
                document.Add(Chunk.NEWLINE);

                // Таблиця витрат
                Paragraph expensesHeader = new Paragraph("ДЕТАЛІЗАЦІЯ ВИТРАТ НА ЗАКУПІВЛЮ", headerFont);
                expensesHeader.SpacingBefore = 15; // Відступ перед заголовком таблиці
                expensesHeader.SpacingAfter = 8;   // Відступ після заголовка таблиці
                document.Add(expensesHeader);
                PdfPTable expensesTable = new PdfPTable(4);
                expensesTable.WidthPercentage = 100;
                expensesTable.SetWidths(new float[] { 1.5f, 3f, 1.5f, 2f });

                // Заголовки стовпців
                AddCell(expensesTable, "Дата", boldFont);
                AddCell(expensesTable, "Товар", boldFont);
                AddCell(expensesTable, "Кількість", boldFont);
                AddCell(expensesTable, "Сума", boldFont);

                // Дані витрат
                decimal totalExpense = 0;
                foreach (DataRow row in reportData.ExpensesData.Rows)
                {
                    AddCell(expensesTable, Convert.ToDateTime(row["OrderDate"]).ToString("dd.MM.yyyy"), normalFont);
                    AddCell(expensesTable, row["ProductName"].ToString(), normalFont);
                    AddCell(expensesTable, row["QuantityOrdered"].ToString(), normalFont);

                    decimal expense = Convert.ToDecimal(row["TotalExpense"]);
                    totalExpense += expense;
                    AddCell(expensesTable, expense.ToString("N2") + " грн", normalFont);
                }

                // Підсумковий рядок
                AddCell(expensesTable, "РАЗОМ:", boldFont);
                AddCell(expensesTable, "", boldFont);
                AddCell(expensesTable, "", boldFont);
                AddCell(expensesTable, totalExpense.ToString("N2") + " грн", boldFont);

                document.Add(expensesTable);
                document.Close();

                // Відкриваємо PDF
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не вдалося відкрити PDF: {ex.Message}", "Помилка");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при створенні PDF: {ex.Message}", "Помилка");
            }
        }
       

        private string GetSupplierEmail(int supplierId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand("SELECT Email FROM Suppliers WHERE SupplierID = @SupplierID", connection);
                    cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                    return cmd.ExecuteScalar()?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        // Допоміжний метод для додавання комірок з вирівнюванням
        private void AddCell(PdfPTable table, string text, Font font, int horizontalAlignment = Element.ALIGN_CENTER)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = horizontalAlignment;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.PaddingTop = 4;    // Верхній відступ
            cell.PaddingBottom = 4; // Нижній відступ
            cell.PaddingLeft = 3;   // Лівий відступ
            cell.PaddingRight = 3;  // Правий відступ
            table.AddCell(cell);
        }
        private void ViewCartButton_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new ShoppingCartWindow();
            if (cartWindow.ShowDialog() == true)
            {
                // Обробка підтвердження замовлення
                CreateOrdersFromCart();
                ShoppingCart.Instance.Clear();
                LoadOrders();
            }
        }

        private void CreateOrdersFromCart()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Групуємо товари за постачальниками
                    var ordersBySupplier = ShoppingCart.Instance.Items
                        .GroupBy(item => item.SupplierId)
                        .ToList();

                    foreach (var supplierGroup in ordersBySupplier)
                    {
                        foreach (var item in supplierGroup)
                        {
                            SqlCommand cmd = new SqlCommand(
                                "INSERT INTO Orders (ProductID, QuantityOrdered, OrderDate, Status, PurchasePrice, SupplierID, ExpectedDeliveryDate) " +
                                "VALUES (@ProductID, @QuantityOrdered, GETDATE(), 'Нове', @PurchasePrice, @SupplierID, @ExpectedDeliveryDate)",
                                connection);

                            cmd.Parameters.AddWithValue("@ProductID", item.ProductId);
                            cmd.Parameters.AddWithValue("@QuantityOrdered", item.Quantity);
                            cmd.Parameters.AddWithValue("@PurchasePrice", item.PurchasePrice);
                            cmd.Parameters.AddWithValue("@SupplierID", item.SupplierId > 0 ? (object)item.SupplierId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@ExpectedDeliveryDate", item.ExpectedDate);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show($"Створено {ShoppingCart.Instance.Items.Count} замовлень з кошика!", "Успіх");
                ShoppingCart.Instance.Clear();
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при створенні замовлень: {ex.Message}", "Помилка");
            }
        }

        private class FinancialReportData
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DataTable SalesData { get; set; }
            public DataTable ExpensesData { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal TotalExpenses { get; set; }
            public decimal NetProfit { get; set; }
            public int SalesCount { get; set; }
        }
     

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadOrders();
        }
    }
}