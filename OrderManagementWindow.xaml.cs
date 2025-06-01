using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class OrderManagementWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private DataTable productsData;
        private DataTable lowStockProductsData;
        private DataTable ordersData;

        public OrderManagementWindow()
        {
            InitializeComponent();
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
                           c.CategoryName, s.Name AS SupplierName, s.SupplierID,
                           (p.MinQuantity - p.Quantity) AS OrderQuantity
                    FROM Products p
                    JOIN Categories c ON p.CategoryID = c.CategoryID
                    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                    WHERE p.Quantity <= p.MinQuantity
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
                    catch
                    {
                        transaction.Rollback();
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

                var orderDialog = new OrderInputWindow(productName, purchasePrice, currentQuantity, minQuantity, supplierId, supplierName);
                if (orderDialog.ShowDialog() == true)
                {
                    int quantityOrdered = orderDialog.QuantityOrdered;
                    int? selectedSupplierId = orderDialog.SelectedSupplierId;
                    DateTime? expectedDate = orderDialog.ExpectedDate;

                    CreateOrder(productId, quantityOrdered, purchasePrice, selectedSupplierId, expectedDate);

                    // Оновлюємо постачальника товару
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
                int ordersCreated = 0;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    foreach (DataRow row in lowStockProductsData.Rows)
                    {
                        int productId = Convert.ToInt32(row["ProductID"]);
                        int currentQuantity = Convert.ToInt32(row["Quantity"]);
                        int minQuantity = Convert.ToInt32(row["MinQuantity"]);

                        // Розрахунок необхідної кількості
                        int orderQuantity = minQuantity - currentQuantity;

                        // Перевірка, чи дійсно потрібне замовлення
                        if (orderQuantity <= 0) continue;

                        decimal purchasePrice = Convert.ToDecimal(row["PurchasePrice"]);
                        int? supplierId = row["SupplierID"] != DBNull.Value
                            ? Convert.ToInt32(row["SupplierID"])
                            : (int?)null;

                        // Очікувана дата поставки (наприклад, через 7 днів)
                        DateTime expectedDate = DateTime.Now.AddDays(7);

                        // Створення замовлення з правильним статусом
                        SqlCommand cmd = new SqlCommand(
                            @"INSERT INTO Orders 
                    (ProductID, QuantityOrdered, OrderDate, Status, 
                     PurchasePrice, SupplierID, ExpectedDeliveryDate) 
                    VALUES 
                    (@ProductID, @QuantityOrdered, GETDATE(), 'Нове', 
                     @PurchasePrice, @SupplierID, @ExpectedDeliveryDate)",
                            connection);

                        cmd.Parameters.AddWithValue("@ProductID", productId);
                        cmd.Parameters.AddWithValue("@QuantityOrdered", orderQuantity);
                        cmd.Parameters.AddWithValue("@PurchasePrice", purchasePrice);
                        cmd.Parameters.AddWithValue("@SupplierID", (object)supplierId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ExpectedDeliveryDate", expectedDate);

                        cmd.ExecuteNonQuery();
                        ordersCreated++;
                    }
                }

                MessageBox.Show($"Створено {ordersCreated} автоматичних замовлень!\n" +
                              $"Кожне замовлення на кількість, необхідну для досягнення мінімального запасу.",
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

        // ДОДАНО МЕТОД ОНОВЛЕННЯ В БД
      

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadOrders();
        }
    }
}