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

            LoadData();
            InitializeCharts();
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
        }

        private void UpdateCharts()
        {
            // Додаємо перевірку на null
            if (salesData == null || salesData.Rows.Count == 0)
            {
                // Очищаємо графіки, якщо немає даних
                SalesChartSeries?.Clear();
                TopProductsSeries?.Clear();
                OnPropertyChanged(nameof(SalesChartSeries));
                OnPropertyChanged(nameof(TopProductsSeries));
                return;
            }

            try
            {
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
                Font titleFont = new Font(baseFont, 16, Font.BOLD, BaseColor.DARK_GRAY);
                Font headerFont = new Font(baseFont, 12, Font.BOLD, BaseColor.WHITE);
                Font normalFont = new Font(baseFont, 10, Font.NORMAL, BaseColor.BLACK);
                Font boldFont = new Font(baseFont, 10, Font.BOLD, BaseColor.BLACK);

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    // Вертикальна орієнтація A4
                    Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, stream);

                    // Відкриваємо документ
                    document.Open();

                    // Додаємо заголовок
                    Paragraph title = new Paragraph("Деталізований звіт про продажі", titleFont);
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

                    // Додаємо статистику
                    decimal totalRevenue = salesData.AsEnumerable().Sum(r => Convert.ToDecimal(r["TotalAmount"]));
                    int totalItems = salesData.AsEnumerable().Sum(r => Convert.ToInt32(r["QuantitySold"]));

                    Paragraph stats = new Paragraph(
                        $"Загальна кількість продажів: {salesData.Rows.Count}\n" +
                        $"Загальна кількість товарів: {totalItems}\n" +
                        $"Загальний дохід: {totalRevenue.ToString("N2")} грн\n\n",
                        boldFont);
                    document.Add(stats);

                    // Основна таблиця
                    PdfPTable table = new PdfPTable(6);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 0.5f, 3f, 2f, 1f, 1.5f, 2f });
                    table.SpacingBefore = 10f;

                    // Заголовки таблиці
                    AddPdfCell(table, "№", headerFont, BaseColor.GRAY);
                    AddPdfCell(table, "Назва товару", headerFont, BaseColor.GRAY);
                    AddPdfCell(table, "Категорія", headerFont, BaseColor.GRAY);
                    AddPdfCell(table, "К-сть", headerFont, BaseColor.GRAY);
                    AddPdfCell(table, "Ціна за од.", headerFont, BaseColor.GRAY);
                    AddPdfCell(table, "Загальна сума", headerFont, BaseColor.GRAY);

                    // Заповнення даними
                    int rowNum = 1;
                    foreach (DataRow row in salesData.Rows)
                    {
                        AddPdfCell(table, rowNum.ToString(), normalFont);
                        AddPdfCell(table, row["ProductName"].ToString(), normalFont);
                        AddPdfCell(table, row["CategoryName"].ToString(), normalFont);
                        AddPdfCell(table, row["QuantitySold"].ToString(), normalFont, alignment: Element.ALIGN_RIGHT);

                        decimal price = Convert.ToDecimal(row["TotalAmount"]) / Convert.ToInt32(row["QuantitySold"]);
                        AddPdfCell(table, price.ToString("N2") + " грн", normalFont, alignment: Element.ALIGN_RIGHT);
                        AddPdfCell(table, Convert.ToDecimal(row["TotalAmount"]).ToString("N2") + " грн", normalFont, alignment: Element.ALIGN_RIGHT);

                        rowNum++;
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
                        Paragraph topTitle = new Paragraph("\nТоп-5 товарів за продажами:", boldFont);
                        topTitle.SpacingBefore = 20f;
                        document.Add(topTitle);

                        PdfPTable topTable = new PdfPTable(4);
                        topTable.WidthPercentage = 100;
                        topTable.SetWidths(new float[] { 0.5f, 3f, 2f, 2f });

                        AddPdfCell(topTable, "№", headerFont, BaseColor.GRAY);
                        AddPdfCell(topTable, "Товар", headerFont, BaseColor.GRAY);
                        AddPdfCell(topTable, "Категорія", headerFont, BaseColor.GRAY);
                        AddPdfCell(topTable, "Сума продажів", headerFont, BaseColor.GRAY);

                        int topNum = 1;
                        foreach (var product in topProducts)
                        {
                            AddPdfCell(topTable, topNum.ToString(), normalFont);
                            AddPdfCell(topTable, product.ProductName, normalFont);
                            AddPdfCell(topTable, product.Category, normalFont);
                            AddPdfCell(topTable, product.Total.ToString("N2") + " грн", normalFont, alignment: Element.ALIGN_RIGHT);
                            topNum++;
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

        private void AddPdfCell(PdfPTable table, string text, Font font, BaseColor bgColor = null, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = alignment;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 5;
            cell.BorderWidth = 0.5f;
            cell.BorderColor = BaseColor.LIGHT_GRAY;

            if (bgColor != null)
            {
                cell.BackgroundColor = bgColor;
            }

            table.AddCell(cell);
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
                string searchText = SearchTextBox.Text.Trim();
                if (searchText.Equals("Введіть пошук...", StringComparison.OrdinalIgnoreCase))
                    searchText = "";

                string categoryFilter = "";
                string selectedCategory = CategoryFilterComboBox.SelectedItem?.ToString();

                if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "Всі категорії")
                {
                    // Фільтруємо по імені категорії
                    categoryFilter = $"[CategoryName] = '{selectedCategory}'";
                }

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    dataView.RowFilter = categoryFilter;
                }
                else
                {
                    string searchFilter = $"(Name LIKE '%{searchText}%' OR CategoryName LIKE '%{searchText}%')";
                    dataView.RowFilter = string.IsNullOrEmpty(categoryFilter)
                        ? searchFilter
                        : $"{categoryFilter} AND {searchFilter}";
                }
            }
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
            if (CategoryFilterComboBox.SelectedItem == null) return;

            try
            {
                ApplyProductFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при фільтрації: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Brushes.Black;

            string status = value.ToString().ToLower();
            return status switch
            {
                "в наявності" => Brushes.Green,
                "закінчується" => Brushes.Orange,
                "немає в наявності" => Brushes.Red,
                _ => Brushes.Black
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PdfPageEventHandler : IPdfPageEvent
    {
        private readonly string _reportTitle;

        public PdfPageEventHandler(string reportTitle)
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
}