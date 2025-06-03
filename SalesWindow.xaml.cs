using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Diagnostics;
using System.Windows.Data;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using LiveCharts;
using LiveCharts.Wpf;

namespace WpfApp1
{
    public partial class SalesWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private DataTable salesData;
        private DataTable productsData;

        public SeriesCollection SalesChartSeries { get; set; }
        public List<string> SalesDates { get; set; }
        public SeriesCollection TopProductsSeries { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public SalesWindow()
        {
            InitializeComponent();
            DataContext = this;

            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Today;

            InitializeCharts();
            LoadData();
            LoadCategories();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                LoadProducts();
                LoadSalesHistory();
                UpdateCharts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при завантаженні даних: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT CategoryName FROM Categories ORDER BY CategoryName";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    SqlDataReader reader = cmd.ExecuteReader();

                    CategoryFilterComboBox.Items.Clear();

                    // Додаємо опцію "Всі категорії"
                    CategoryFilterComboBox.Items.Add("Всі категорії");

                    // Додаємо категорії з бази даних
                    while (reader.Read())
                    {
                        CategoryFilterComboBox.Items.Add(reader["CategoryName"].ToString());
                    }

                    CategoryFilterComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження категорій: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT p.ProductID, p.Name, p.Quantity, p.Price, c.CategoryName, 
                               CASE WHEN p.Quantity <= 0 THEN 'Немає в наявності'
                                    WHEN p.Quantity < 10 THEN 'Закінчується'
                                    ELSE 'В наявності' END AS Status
                        FROM Products p
                        JOIN Categories c ON p.CategoryID = c.CategoryID
                        ORDER BY p.Name";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    productsData = new DataTable();
                    adapter.Fill(productsData);

                    ProductDataGrid.ItemsSource = productsData.DefaultView;
                    ApplyProductFilter();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження товарів: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSalesHistory()
        {
            try
            {
                DateTime startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
                DateTime endDate = EndDatePicker.SelectedDate ?? DateTime.Today;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            s.SaleID, 
                            p.Name AS ProductName, 
                            s.QuantitySold, 
                            s.TotalAmount,
                            FORMAT(s.SaleDate, 'dd.MM.yyyy HH:mm') AS SaleDate,
                            c.CategoryName
                        FROM Sales s
                        JOIN Products p ON s.ProductID = p.ProductID
                        JOIN Categories c ON p.CategoryID = c.CategoryID
                        WHERE s.SaleDate BETWEEN @StartDate AND @EndDate
                        ORDER BY s.SaleDate DESC";

                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate.AddDays(1));

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    salesData = new DataTable();
                    adapter.Fill(salesData);

                    SalesHistoryDataGrid.ItemsSource = salesData.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження історії продажів: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeCharts()
        {
            SalesChartSeries = new SeriesCollection();
            TopProductsSeries = new SeriesCollection();
            SalesDates = new List<string>();

            OnPropertyChanged(nameof(SalesChartSeries));
            OnPropertyChanged(nameof(TopProductsSeries));
            OnPropertyChanged(nameof(SalesDates));
        }

        private void UpdateCharts()
        {
            try
            {
                // Перевірка на наявність даних
                if (salesData == null || salesData.Rows.Count == 0)
                {
                    SalesChartSeries = new SeriesCollection();
                    TopProductsSeries = new SeriesCollection();
                    SalesDates = new List<string>();

                    OnPropertyChanged(nameof(SalesChartSeries));
                    OnPropertyChanged(nameof(TopProductsSeries));
                    OnPropertyChanged(nameof(SalesDates));
                    return;
                }

                var filteredData = salesData.AsEnumerable()
                    .Where(r => r["SaleDate"] != DBNull.Value &&
                               r["TotalAmount"] != DBNull.Value &&
                               r["ProductName"] != null)
                    .ToList();

                var dailySales = filteredData
                    .GroupBy(r => {
                        DateTime saleDate;
                        if (DateTime.TryParseExact(r["SaleDate"].ToString(), "dd.MM.yyyy HH:mm",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out saleDate))
                        {
                            return saleDate.Date;
                        }
                        return DateTime.MinValue;
                    })
                    .Where(g => g.Key != DateTime.MinValue)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalAmount = g.Sum(r => Convert.ToDecimal(r["TotalAmount"]))
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                SalesDates = dailySales.Select(x => x.Date.ToString("dd.MM")).ToList();

                SalesChartSeries = new SeriesCollection();
                if (dailySales.Any())
                {
                    SalesChartSeries.Add(new LineSeries
                    {
                        Title = "Продажі",
                        Values = new ChartValues<decimal>(dailySales.Select(x => x.TotalAmount)),
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10,
                        LineSmoothness = 0.5
                    });
                }

                TopProductsSeries = new SeriesCollection();
                var topProducts = filteredData
                    .GroupBy(r => r["ProductName"].ToString())
                    .Select(g => new
                    {
                        ProductName = g.Key,
                        TotalSales = g.Sum(r => Convert.ToDecimal(r["TotalAmount"]))
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .Take(5)
                    .ToList();

                foreach (var product in topProducts)
                {
                    TopProductsSeries.Add(new PieSeries
                    {
                        Title = product.ProductName,
                        Values = new ChartValues<decimal> { product.TotalSales },
                        DataLabels = true,
                        LabelPosition = PieLabelPosition.OutsideSlice
                    });
                }

                OnPropertyChanged(nameof(SalesChartSeries));
                OnPropertyChanged(nameof(SalesDates));
                OnPropertyChanged(nameof(TopProductsSeries));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення графіків: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // Створюємо пусті колекції при помилці
                SalesChartSeries = new SeriesCollection();
                TopProductsSeries = new SeriesCollection();
                SalesDates = new List<string>();

                OnPropertyChanged(nameof(SalesChartSeries));
                OnPropertyChanged(nameof(TopProductsSeries));
                OnPropertyChanged(nameof(SalesDates));
            }
        }

        private void BtnSell_Click(object sender, RoutedEventArgs e)
        {
            if (ProductDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, оберіть товар для продажу.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DataRowView row = (DataRowView)ProductDataGrid.SelectedItem;
                int productId = Convert.ToInt32(row["ProductID"]);
                string productName = row["Name"].ToString();
                decimal unitPrice = Convert.ToDecimal(row["Price"]);
                int availableQuantity = Convert.ToInt32(row["Quantity"]);

                if (availableQuantity <= 0)
                {
                    MessageBox.Show("Обраний товар відсутній на складі!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var inputDialog = new QuantityInputWindow(productName, availableQuantity, unitPrice);
                if (inputDialog.ShowDialog() == true)
                {
                    decimal totalAmount = inputDialog.QuantitySold * unitPrice;

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (SqlTransaction transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Products SET Quantity = Quantity - @QuantitySold WHERE ProductID = @ProductID",
                                    connection, transaction);
                                updateCmd.Parameters.AddWithValue("@QuantitySold", inputDialog.QuantitySold);
                                updateCmd.Parameters.AddWithValue("@ProductID", productId);
                                updateCmd.ExecuteNonQuery();

                                SqlCommand insertCmd = new SqlCommand(
                                    "INSERT INTO Sales (ProductID, QuantitySold, TotalAmount, SaleDate) " +
                                    "VALUES (@ProductID, @QuantitySold, @TotalAmount, GETDATE())",
                                    connection, transaction);

                                insertCmd.Parameters.AddWithValue("@ProductID", productId);
                                insertCmd.Parameters.AddWithValue("@QuantitySold", inputDialog.QuantitySold);
                                insertCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                                insertCmd.ExecuteNonQuery();

                                transaction.Commit();

                                MessageBox.Show(
                                    $"Продаж {inputDialog.QuantitySold} од. товару '{productName}' на суму {totalAmount:0.00} грн успішно завершено!",
                                    "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

                                LoadData();
                                UpdateMainWindowDailySales();

                                // Додано: Оновлення головного вікна
                                if (Application.Current.MainWindow is MainWindow mainWindow)
                                {
                                    mainWindow.RefreshDashboard();
                                }
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                MessageBox.Show($"Помилка при продажу: {ex.Message}\n\nЗміни скасовано.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMainWindowDailySales()
        {
            // Оновлення продажів у головному вікні
            var mainWindow = Application.Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault();

            if (mainWindow != null)
            {
                mainWindow.UpdateDailySales();
            }
        }

        private void BtnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (salesData == null || salesData.Rows.Count == 0)
                {
                    MessageBox.Show("Немає даних для формування звіту за обраний період.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Звіт_продажів_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    Title = "Зберегти звіт як PDF"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    GeneratePdfReport(saveDialog.FileName);

                    if (MessageBox.Show("Звіт успішно згенеровано! Відкрити файл?", "Успіх",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при генерації звіту: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GeneratePdfReport(string filePath)
        {
            try
            {
                // Створюємо стилі для тексту
                BaseFont baseFont = BaseFont.CreateFont("c:/windows/fonts/arial.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font titleFont = new Font(baseFont, 16, Font.BOLD, BaseColor.BLACK); // Чорний заголовок
                Font headerFont = new Font(baseFont, 12, Font.BOLD, BaseColor.WHITE); // Білий колір тексту для заголовків
                Font normalFont = new Font(baseFont, 10, Font.NORMAL, BaseColor.BLACK); // Чорний текст
                Font boldFont = new Font(baseFont, 10, Font.BOLD, BaseColor.BLACK); // Чорний жирний текст для підрахунків

                // Кольори для таблиць
                BaseColor headerColor = new BaseColor(34, 139, 34); // Зелений для заголовків
                BaseColor lightGreen = new BaseColor(220, 255, 220); // Світло-зелений фон
                BaseColor borderColor = new BaseColor(150, 200, 150); // Світло-зелена рамка

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, stream);
                    document.Open();

                    // Додаємо заголовок (тепер чорний)
                    Paragraph title = new Paragraph("Звіт про продажі", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 20;
                    document.Add(title);

                    // Додаємо інформацію про період
                    DateTime startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
                    DateTime endDate = EndDatePicker.SelectedDate ?? DateTime.Today;

                    Paragraph periodInfo = new Paragraph(
                        $"Період: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}\n" +
                        $"Дата формування: {DateTime.Now:dd.MM.yyyy HH:mm}\n\n",
                        normalFont);
                    document.Add(periodInfo);

                    // Додаємо статистику (тепер чорний)
                    decimal totalRevenue = salesData.AsEnumerable().Sum(r => Convert.ToDecimal(r["TotalAmount"]));
                    int totalItems = salesData.AsEnumerable().Sum(r => Convert.ToInt32(r["QuantitySold"]));

                    Paragraph stats = new Paragraph(
                        $"Загальна кількість продажів: {salesData.Rows.Count}\n" +
                        $"Загальна кількість товарів: {totalItems}\n" +
                        $"Загальний дохід: {totalRevenue.ToString("N2")} грн\n\n",
                        boldFont);
                    document.Add(stats);

                    // Решта коду залишається без змін...
                    // Основна таблиця
                    PdfPTable table = new PdfPTable(6);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 0.5f, 3f, 2f, 1f, 1.5f, 2f });
                    table.SpacingBefore = 10f;
                    table.SpacingAfter = 20f;

                    // Заголовки таблиці (білий текст на зеленому фоні)
                    AddPdfCell(table, "№", headerFont, headerColor, borderColor);
                    AddPdfCell(table, "Назва товару", headerFont, headerColor, borderColor);
                    AddPdfCell(table, "Категорія", headerFont, headerColor, borderColor);
                    AddPdfCell(table, "К-сть", headerFont, headerColor, borderColor);
                    AddPdfCell(table, "Ціна за од.", headerFont, headerColor, borderColor);
                    AddPdfCell(table, "Загальна сума", headerFont, headerColor, borderColor);

                    // Заповнення даними (чорний текст на світло-зеленому/білому фоні)
                    int rowNum = 1;
                    bool alternate = false;
                    foreach (DataRow row in salesData.Rows)
                    {
                        BaseColor cellColor = alternate ? lightGreen : BaseColor.WHITE;

                        AddPdfCell(table, rowNum.ToString(), normalFont, cellColor, borderColor);
                        AddPdfCell(table, row["ProductName"].ToString(), normalFont, cellColor, borderColor);
                        AddPdfCell(table, row["CategoryName"].ToString(), normalFont, cellColor, borderColor);
                        AddPdfCell(table, row["QuantitySold"].ToString(), normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);

                        decimal price = Convert.ToDecimal(row["TotalAmount"]) / Convert.ToInt32(row["QuantitySold"]);
                        AddPdfCell(table, price.ToString("N2") + " грн", normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);
                        AddPdfCell(table, Convert.ToDecimal(row["TotalAmount"]).ToString("N2") + " грн", normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);

                        rowNum++;
                        alternate = !alternate;
                    }

                    document.Add(table);

                    // Топ товари
                    var topProducts = salesData.AsEnumerable()
                        .GroupBy(r => new { Name = r["ProductName"].ToString(), Category = r["CategoryName"].ToString() })
                        .Select(g => new {
                            ProductName = g.Key.Name,
                            Category = g.Key.Category,
                            Total = g.Sum(r => Convert.ToDecimal(r["TotalAmount"]))
                        })
                        .OrderByDescending(x => x.Total)
                        .Take(5)
                        .ToList();

                    if (topProducts.Count > 0)
                    {
                        Paragraph topTitle = new Paragraph("Топ-5 товарів за продажами:", boldFont);
                        topTitle.SpacingBefore = 30f;
                        topTitle.SpacingAfter = 10f;
                        document.Add(topTitle);

                        PdfPTable topTable = new PdfPTable(4);
                        topTable.WidthPercentage = 100;
                        topTable.SetWidths(new float[] { 0.5f, 3f, 2f, 2f });
                        topTable.SpacingAfter = 20f;

                        // Заголовки топ-таблиці (білий текст на зеленому фоні)
                        AddPdfCell(topTable, "№", headerFont, headerColor, borderColor);
                        AddPdfCell(topTable, "Товар", headerFont, headerColor, borderColor);
                        AddPdfCell(topTable, "Категорія", headerFont, headerColor, borderColor);
                        AddPdfCell(topTable, "Сума продажів", headerFont, headerColor, borderColor);

                        // Заповнення топ-таблиці (чорний текст на світло-зеленому/білому фоні)
                        int topNum = 1;
                        alternate = false;
                        foreach (var product in topProducts)
                        {
                            BaseColor cellColor = alternate ? lightGreen : BaseColor.WHITE;

                            AddPdfCell(topTable, topNum.ToString(), normalFont, cellColor, borderColor);
                            AddPdfCell(topTable, product.ProductName, normalFont, cellColor, borderColor);
                            AddPdfCell(topTable, product.Category, normalFont, cellColor, borderColor);
                            AddPdfCell(topTable, product.Total.ToString("N2") + " грн", normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);

                            topNum++;
                            alternate = !alternate;
                        }

                        document.Add(topTable);
                    }

                    document.Close();
                }

                MessageBox.Show("Звіт успішно згенеровано!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при генерації звіту: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Оновлений метод для додавання комірок з можливістю вказати колір рамки
        private void AddPdfCell(PdfPTable table, string text, Font font,
                               BaseColor bgColor = null, BaseColor borderColor = null,
                               int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = alignment;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 5;
            cell.BorderWidth = 0.5f;
            cell.BorderColor = borderColor ?? BaseColor.LIGHT_GRAY;

            if (bgColor != null)
            {
                cell.BackgroundColor = bgColor;
            }

            table.AddCell(cell);
        }

        private void BtnGenerateAnalyticalReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (salesData == null || salesData.Rows.Count == 0)
                {
                    MessageBox.Show("Немає даних для формування звіту за обраний період.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Аналітичний_звіт_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    Title = "Зберегти аналітичний звіт як PDF"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    GenerateAnalyticalPdfReport(saveDialog.FileName);

                    if (MessageBox.Show("Аналітичний звіт успішно згенеровано! Відкрити файл?", "Успіх",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при генерації аналітичного звіту: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для генерації аналітичного PDF-звіту
        private void GenerateAnalyticalPdfReport(string filePath)
        {
            try
            {
                DateTime startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
                DateTime endDate = EndDatePicker.SelectedDate ?? DateTime.Today;

                // Створюємо стилі для тексту
                BaseFont baseFont = BaseFont.CreateFont("c:/windows/fonts/arial.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                // Кольори тексту
                BaseColor darkGray = new BaseColor(50, 50, 50);    // Основний текст
                BaseColor softBlack = new BaseColor(70, 70, 70);   // Заголовки
                BaseColor white = BaseColor.WHITE;                 // Текст в заголовках таблиць

                // Кольори для таблиць
                BaseColor headerColor = new BaseColor(34, 139, 34); // Зелений фон заголовків
                BaseColor lightGreen = new BaseColor(220, 255, 220); // Світло-зелений фон
                BaseColor borderColor = new BaseColor(150, 200, 150); // Світло-зелена рамка
                BaseColor lightGray = new BaseColor(240, 240, 240); // Світло-сірий для підсумків

                // Шрифти
                Font titleFont = new Font(baseFont, 16, Font.BOLD, softBlack);        // Заголовок звіту
                Font sectionFont = new Font(baseFont, 12, Font.BOLD, softBlack);     // Назви секцій
                Font tableHeaderFont = new Font(baseFont, 12, Font.BOLD, white);     // Заголовки стовпців (білий)
                Font normalFont = new Font(baseFont, 10, Font.NORMAL, darkGray);     // Основний текст
                Font boldFont = new Font(baseFont, 10, Font.BOLD, softBlack);        // Жирний текст

                // Змінна для чергування кольорів рядків
                bool alternate = false;

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    Document document = new Document(PageSize.A4.Rotate(), 30, 30, 50, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, stream);

                    writer.PageEvent = new PdfPageEventHandler("Аналітичний звіт про продажі", startDate, endDate);

                    document.Open();

                    // Додаємо заголовок
                    Paragraph title = new Paragraph("Аналітичний звіт про продажі", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 20;
                    document.Add(title);

                    // Додаємо інформацію про період
                    Paragraph periodInfo = new Paragraph(
                        $"Період: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}\n" +
                        $"Дата формування: {DateTime.Now:dd.MM.yyyy HH:mm}\n\n",
                        normalFont);
                    document.Add(periodInfo);

                    // Загальна статистика
                    decimal totalRevenue = salesData.AsEnumerable().Sum(r => Convert.ToDecimal(r["TotalAmount"]));
                    int totalItems = salesData.AsEnumerable().Sum(r => Convert.ToInt32(r["QuantitySold"]));
                    int totalSales = salesData.Rows.Count;

                    Paragraph statsTitle = new Paragraph("Загальна статистика продажів", sectionFont);
                    statsTitle.SpacingBefore = 15f;
                    statsTitle.SpacingAfter = 10f;
                    document.Add(statsTitle);

                    PdfPTable statsTable = new PdfPTable(3);
                    statsTable.WidthPercentage = 100;
                    statsTable.SetWidths(new float[] { 1f, 1f, 1f });

                    // Заголовки (білий текст на зеленому фоні)
                    AddPdfCell(statsTable, "Загальний дохід", tableHeaderFont, headerColor, borderColor);
                    AddPdfCell(statsTable, "Загальна кількість продажів", tableHeaderFont, headerColor, borderColor);
                    AddPdfCell(statsTable, "Загальна кількість товарів", tableHeaderFont, headerColor, borderColor);

                    // Дані (темний текст на світло-зеленому/білому фоні)
                    AddPdfCell(statsTable, $"{totalRevenue.ToString("N2")} грн", normalFont, lightGreen, borderColor);
                    AddPdfCell(statsTable, totalSales.ToString(), normalFont, BaseColor.WHITE, borderColor);
                    AddPdfCell(statsTable, totalItems.ToString(), normalFont, lightGreen, borderColor);

                    document.Add(statsTable);

                    // Динаміка продажів по дням
                    var dailySales = salesData.AsEnumerable()
                        .GroupBy(r => {
                            DateTime saleDate;
                            if (DateTime.TryParseExact(r["SaleDate"].ToString(), "dd.MM.yyyy HH:mm",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out saleDate))
                            {
                                return saleDate.Date;
                            }
                            return DateTime.MinValue;
                        })
                        .Where(g => g.Key != DateTime.MinValue)
                        .Select(g => new
                        {
                            Date = g.Key,
                            SalesCount = g.Count(),
                            ItemsSold = g.Sum(r => Convert.ToInt32(r["QuantitySold"])),
                            TotalAmount = g.Sum(r => Convert.ToDecimal(r["TotalAmount"]))
                        })
                        .OrderBy(x => x.Date)
                        .ToList();

                    if (dailySales.Any())
                    {
                        Paragraph dailyTitle = new Paragraph("\nДинаміка продажів по дням", sectionFont);
                        dailyTitle.SpacingBefore = 20f;
                        dailyTitle.SpacingAfter = 10f;
                        document.Add(dailyTitle);

                        PdfPTable dailyTable = new PdfPTable(4);
                        dailyTable.WidthPercentage = 100;
                        dailyTable.SetWidths(new float[] { 2f, 1.5f, 1.5f, 2f });

                        // Заголовки (білий текст на зеленому фоні)
                        AddPdfCell(dailyTable, "Дата", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(dailyTable, "Кількість продажів", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(dailyTable, "Кількість товарів", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(dailyTable, "Загальна сума", tableHeaderFont, headerColor, borderColor);

                        // Дані (темний текст на світло-зеленому/білому фоні)
                        alternate = false;
                        foreach (var day in dailySales)
                        {
                            BaseColor cellColor = alternate ? lightGreen : BaseColor.WHITE;

                            AddPdfCell(dailyTable, day.Date.ToString("dd.MM.yyyy"), normalFont, cellColor, borderColor);
                            AddPdfCell(dailyTable, day.SalesCount.ToString(), normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);
                            AddPdfCell(dailyTable, day.ItemsSold.ToString(), normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);
                            AddPdfCell(dailyTable, day.TotalAmount.ToString("N2") + " грн", normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);

                            alternate = !alternate;
                        }

                        // Підсумковий рядок
                        AddPdfCell(dailyTable, "Всього:", boldFont, lightGray, borderColor);
                        AddPdfCell(dailyTable, dailySales.Sum(d => d.SalesCount).ToString(), boldFont, lightGray, borderColor, Element.ALIGN_RIGHT);
                        AddPdfCell(dailyTable, dailySales.Sum(d => d.ItemsSold).ToString(), boldFont, lightGray, borderColor, Element.ALIGN_RIGHT);
                        AddPdfCell(dailyTable, dailySales.Sum(d => d.TotalAmount).ToString("N2") + " грн", boldFont, lightGray, borderColor, Element.ALIGN_RIGHT);

                        document.Add(dailyTable);
                    }

                    // Аналіз по категоріях
                    var salesByCategory = salesData.AsEnumerable()
                        .GroupBy(r => r["CategoryName"].ToString())
                        .Select(g => new
                        {
                            Category = g.Key,
                            SalesCount = g.Count(),
                            ItemsSold = g.Sum(r => Convert.ToInt32(r["QuantitySold"])),
                            TotalAmount = g.Sum(r => Convert.ToDecimal(r["TotalAmount"])),
                            Percentage = (decimal)g.Sum(r => Convert.ToDecimal(r["TotalAmount"])) / totalRevenue * 100
                        })
                        .OrderByDescending(x => x.TotalAmount)
                        .ToList();

                    if (salesByCategory.Any())
                    {
                        Paragraph categoryTitle = new Paragraph("\nАналіз продажів по категоріях", sectionFont);
                        categoryTitle.SpacingBefore = 20f;
                        categoryTitle.SpacingAfter = 10f;
                        document.Add(categoryTitle);

                        PdfPTable categoryTable = new PdfPTable(5);
                        categoryTable.WidthPercentage = 100;
                        categoryTable.SetWidths(new float[] { 3f, 1.5f, 1.5f, 2f, 2f });

                        // Заголовки (білий текст на зеленому фоні)
                        AddPdfCell(categoryTable, "Категорія", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(categoryTable, "Кількість продажів", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(categoryTable, "Кількість товарів", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(categoryTable, "Загальна сума", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(categoryTable, "Частка від загального доходу", tableHeaderFont, headerColor, borderColor);

                        // Дані (темний текст на світло-зеленому/білому фоні)
                        alternate = false;
                        foreach (var category in salesByCategory)
                        {
                            BaseColor cellColor = alternate ? lightGreen : BaseColor.WHITE;

                            AddPdfCell(categoryTable, category.Category, normalFont, cellColor, borderColor);
                            AddPdfCell(categoryTable, category.SalesCount.ToString(), normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);
                            AddPdfCell(categoryTable, category.ItemsSold.ToString(), normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);
                            AddPdfCell(categoryTable, category.TotalAmount.ToString("N2") + " грн", normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);
                            AddPdfCell(categoryTable, category.Percentage.ToString("N1") + "%", normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);

                            alternate = !alternate;
                        }

                        document.Add(categoryTable);
                    }

                    // Топ товарів
                    var topProducts = salesData.AsEnumerable()
                        .GroupBy(r => new { Name = r["ProductName"].ToString(), Category = r["CategoryName"].ToString() })
                        .Select(g => new
                        {
                            ProductName = g.Key.Name,
                            Category = g.Key.Category,
                            SalesCount = g.Count(),
                            ItemsSold = g.Sum(r => Convert.ToInt32(r["QuantitySold"])),
                            TotalAmount = g.Sum(r => Convert.ToDecimal(r["TotalAmount"]))
                        })
                        .OrderByDescending(x => x.TotalAmount)
                        .Take(10)
                        .ToList();

                    if (topProducts.Any())
                    {
                        Paragraph topProductsTitle = new Paragraph("\nТоп-10 товарів за обсягом продажів", sectionFont);
                        topProductsTitle.SpacingBefore = 20f;
                        topProductsTitle.SpacingAfter = 10f;
                        document.Add(topProductsTitle);

                        PdfPTable topProductsTable = new PdfPTable(5);
                        topProductsTable.WidthPercentage = 100;
                        topProductsTable.SetWidths(new float[] { 0.5f, 3f, 2f, 1.5f, 2f });

                        // Заголовки (білий текст на зеленому фоні)
                        AddPdfCell(topProductsTable, "№", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(topProductsTable, "Товар", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(topProductsTable, "Категорія", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(topProductsTable, "Кількість проданих", tableHeaderFont, headerColor, borderColor);
                        AddPdfCell(topProductsTable, "Загальна сума", tableHeaderFont, headerColor, borderColor);

                        // Дані (темний текст на світло-зеленому/білому фоні)
                        int counter = 1;
                        alternate = false;
                        foreach (var product in topProducts)
                        {
                            BaseColor cellColor = alternate ? lightGreen : BaseColor.WHITE;

                            AddPdfCell(topProductsTable, counter.ToString(), normalFont, cellColor, borderColor);
                            AddPdfCell(topProductsTable, product.ProductName, normalFont, cellColor, borderColor);
                            AddPdfCell(topProductsTable, product.Category, normalFont, cellColor, borderColor);
                            AddPdfCell(topProductsTable, product.ItemsSold.ToString(), normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);
                            AddPdfCell(topProductsTable, product.TotalAmount.ToString("N2") + " грн", normalFont, cellColor, borderColor, Element.ALIGN_RIGHT);

                            counter++;
                            alternate = !alternate;
                        }

                        document.Add(topProductsTable);
                    }

                    document.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при генерації аналітичного звіту: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class PdfPageEventHandler : IPdfPageEvent
        {
            private readonly string _reportTitle;
            private readonly DateTime? _startDate;
            private readonly DateTime? _endDate;

            public PdfPageEventHandler(string reportTitle, DateTime? startDate = null, DateTime? endDate = null)
            {
                _reportTitle = reportTitle;
                _startDate = startDate;
                _endDate = endDate;
            }

            public void OnEndPage(PdfWriter writer, Document document)
            {
                // Хедер
                PdfPTable header = new PdfPTable(1);
                header.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
                header.DefaultCell.Border = Rectangle.NO_BORDER;
                header.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;
                header.DefaultCell.VerticalAlignment = Element.ALIGN_MIDDLE;

                // Використовуємо BaseColor.BLUE замість BaseColor.DARK_BLUE
                PdfPCell cell = new PdfPCell(new Phrase(_reportTitle,
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLUE)));
                cell.Border = Rectangle.NO_BORDER;
                cell.PaddingTop = 10;
                cell.BorderWidthBottom = 1f;
                cell.BorderColorBottom = BaseColor.LIGHT_GRAY;
                header.AddCell(cell);

                if (_startDate != null && _endDate != null)
                {
                    cell = new PdfPCell(new Phrase($"Період: {_startDate:dd.MM.yyyy} - {_endDate:dd.MM.yyyy}",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.DARK_GRAY)));
                    cell.Border = Rectangle.NO_BORDER;
                    cell.PaddingBottom = 5;
                    header.AddCell(cell);
                }

                header.WriteSelectedRows(0, -1, document.LeftMargin, document.PageSize.Height - 20, writer.DirectContent);

                // Футер
                PdfPTable footer = new PdfPTable(1);
                footer.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
                footer.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;

                cell = new PdfPCell(new Phrase($"Сторінка {writer.PageNumber}",
                    FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY)));
                cell.Border = Rectangle.NO_BORDER;
                cell.PaddingBottom = 10;
                footer.AddCell(cell);

                footer.WriteSelectedRows(0, -1, document.LeftMargin, document.BottomMargin, writer.DirectContent);
            }

            // Інші методи інтерфейсу...
            public void OnOpenDocument(PdfWriter writer, Document document) { }
            public void OnCloseDocument(PdfWriter writer, Document document) { }
            public void OnStartPage(PdfWriter writer, Document document) { }
            public void OnParagraph(PdfWriter writer, Document document, float paragraphPosition) { }
            public void OnParagraphEnd(PdfWriter writer, Document document, float paragraphPosition) { }
            public void OnChapter(PdfWriter writer, Document document, float paragraphPosition, Paragraph title) { }
            public void OnChapterEnd(PdfWriter writer, Document document, float paragraphPosition) { }
            public void OnSection(PdfWriter writer, Document document, float paragraphPosition, int depth, Paragraph title) { }
            public void OnSectionEnd(PdfWriter writer, Document document, float paragraphPosition) { }
            public void OnGenericTag(PdfWriter writer, Document document, Rectangle rect, string text) { }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text == "Введіть пошук...")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Введіть пошук...";
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyProductFilter();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text != "Введіть пошук...")
            {
                ApplyProductFilter();
            }
        }

        private void ApplyProductFilter()
        {
            if (ProductDataGrid.ItemsSource is DataView dataView)
            {
                string searchText = SearchTextBox.Text?.Trim() ?? "";
                if (searchText.Equals("Введіть пошук...", StringComparison.OrdinalIgnoreCase))
                    searchText = "";

                string categoryFilter = "";
                string selectedCategory = CategoryFilterComboBox.SelectedItem as string;

                if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "Всі категорії")
                {
                    categoryFilter = $"[CategoryName] = '{EscapeFilterValue(selectedCategory)}'";
                }

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    dataView.RowFilter = categoryFilter;
                }
                else
                {
                    string escapedSearch = EscapeFilterValue(searchText);
                    string searchFilter = $"(Name LIKE '%{escapedSearch}%' OR CategoryName LIKE '%{escapedSearch}%')";

                    dataView.RowFilter = string.IsNullOrEmpty(categoryFilter)
                        ? searchFilter
                        : $"{categoryFilter} AND {searchFilter}";
                }
            }
        }

        // Допоміжний метод для екранування
        private string EscapeFilterValue(string value)
        {
            return value?.Replace("'", "''") ?? "";
        }

        private void ApplyDateFilter_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Дата початку не може бути пізніше дати завершення!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            LoadSalesHistory();
            UpdateCharts();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Today;
            SearchTextBox.Text = "";
            CategoryFilterComboBox.SelectedIndex = 0;

            LoadData();
        }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) // Перевірка, що вікно вже завантажене
            {
                ApplyProductFilter();
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    // Перейменований клас для уникнення конфлікту імен
    public class BasePdfPageEventHandler : IPdfPageEvent
    {
        private readonly string _reportTitle;

        public BasePdfPageEventHandler(string reportTitle)
        {
            _reportTitle = reportTitle;
        }

        public void OnOpenDocument(PdfWriter writer, Document document) { }
        public void OnCloseDocument(PdfWriter writer, Document document) { }
        public void OnStartPage(PdfWriter writer, Document document) { }

        public void OnEndPage(PdfWriter writer, Document document)
        {
            PdfPTable header = new PdfPTable(1);
            header.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
            header.DefaultCell.Border = Rectangle.NO_BORDER;
            header.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;
            header.DefaultCell.VerticalAlignment = Element.ALIGN_MIDDLE;

            PdfPCell cell = new PdfPCell(new Phrase(_reportTitle,
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.DARK_GRAY)));
            cell.Border = Rectangle.NO_BORDER;
            cell.PaddingTop = 10;
            header.AddCell(cell);

            header.WriteSelectedRows(0, -1, document.LeftMargin, document.PageSize.Height - 20, writer.DirectContent);

            PdfPTable footer = new PdfPTable(1);
            footer.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
            footer.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;

            cell = new PdfPCell(new Phrase($"Сторінка {writer.PageNumber}",
                FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY)));
            cell.Border = Rectangle.NO_BORDER;
            cell.PaddingBottom = 10;
            footer.AddCell(cell);

            footer.WriteSelectedRows(0, -1, document.LeftMargin, document.BottomMargin, writer.DirectContent);
        }

        public void OnParagraph(PdfWriter writer, Document document, float paragraphPosition) { }
        public void OnParagraphEnd(PdfWriter writer, Document document, float paragraphPosition) { }
        public void OnChapter(PdfWriter writer, Document document, float paragraphPosition, Paragraph title) { }
        public void OnChapterEnd(PdfWriter writer, Document document, float paragraphPosition) { }
        public void OnSection(PdfWriter writer, Document document, float paragraphPosition, int depth, Paragraph title) { }
        public void OnSectionEnd(PdfWriter writer, Document document, float paragraphPosition) { }
        public void OnGenericTag(PdfWriter writer, Document document, Rectangle rect, string text) { }
    }

    public class Sale
    {
        public int SaleID { get; set; }
        public DateTime SaleDate { get; set; }
        public int ProductID { get; set; }
        public int QuantitySold { get; set; }
        public int TotalAmount { get; set; }
        public int TotalPrice { get; set; }

        // Навігаційна властивість до продукту
        public virtual Product Product { get; set; }
    }
}