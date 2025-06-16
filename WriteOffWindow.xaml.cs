using ClosedXML.Excel; // Для роботи з Excel
using iTextSharp.text; // Для роботи з PDF
using iTextSharp.text.pdf;
using System.Data;
using Microsoft.Data.SqlClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class WriteOffWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";

        public WriteOffWindow()
        {
            InitializeComponent();
            LoadProducts();
            LoadWriteOffHistory();
            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        private void LoadProducts()
        {
            try
            {
                ProductComboBox.Items.Clear();
                string query = "SELECT ProductID, Name, Quantity, Price, CategoryID FROM Products";

                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        ProductComboBox.Items.Add(new
                        {
                            ProductID = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Quantity = reader.GetInt32(2),
                            Price = reader.GetDecimal(3),
                            CategoryID = reader.GetInt32(4)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження товарів: {ex.Message}");
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem != null)
            {
                dynamic selectedProduct = ProductComboBox.SelectedItem;
                AvailableQuantityText.Text = selectedProduct.Quantity.ToString();
                LoadProductInfo(selectedProduct.ProductID);
            }
        }

        private void LoadProductInfo(int productId)
        {
            try
            {
                string query = @"SELECT p.Name, p.Description, c.CategoryName, p.Price
                                FROM Products p
                                JOIN Categories c ON p.CategoryID = c.CategoryID
                                WHERE p.ProductID = @ProductID";

                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductID", productId);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        ProductInfoText.Text = $"{reader["Name"]}\n" +
                                              $"Категорія: {reader["CategoryName"]}\n" +
                                              $"Ціна: {reader["Price"]:N2} грн\n" +
                                              $"Опис: {reader["Description"]}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження інформації: {ex.Message}");
            }
        }

        private void WriteOffButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem == null)
            {
                MessageBox.Show("Оберіть товар");
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введіть коректну кількість");
                return;
            }

            dynamic selectedProduct = ProductComboBox.SelectedItem;
            int productId = selectedProduct.ProductID;
            int availableQuantity = selectedProduct.Quantity;

            if (quantity > availableQuantity)
            {
                MessageBox.Show($"Недостатня кількість товару на складі. Доступно: {availableQuantity}");
                return;
            }

            string reason = (ReasonComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Інше";
            string description = DescriptionTextBox.Text;
            string userName = App.CurrentUser?.Username ?? "Система";

            try
            {
                // Зберегти списання в базі даних
                string insertQuery = @"
                    INSERT INTO WriteOffs (ProductID, Quantity, Reason, Description, WriteOffDate, UserName)
                    VALUES (@ProductID, @Quantity, @Reason, @Description, GETDATE(), @UserName)";

                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@ProductID", productId);
                    command.Parameters.AddWithValue("@Quantity", quantity);
                    command.Parameters.AddWithValue("@Reason", reason);
                    command.Parameters.AddWithValue("@Description", description);
                    command.Parameters.AddWithValue("@UserName", userName);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                // Оновити кількість товару
                string updateQuery = "UPDATE Products SET Quantity = Quantity - @Quantity WHERE ProductID = @ProductID";
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@ProductID", productId);
                    command.Parameters.AddWithValue("@Quantity", quantity);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                MessageBox.Show("Товар успішно списано");
                ClearForm();
                LoadWriteOffHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при списанні товару: {ex.Message}");
            }
        }

        private void ClearForm()
        {
            ProductComboBox.SelectedIndex = -1;
            QuantityTextBox.Text = "";
            ReasonComboBox.SelectedIndex = -1;
            DescriptionTextBox.Text = "";
            AvailableQuantityText.Text = "";
            ProductInfoText.Text = "";
        }

        private void LoadWriteOffHistory()
        {
            try
            {
                DateTime startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
                DateTime endDate = EndDatePicker.SelectedDate ?? DateTime.Today;
                string searchText = SearchTextBox.Text;

                string query = @"
                    SELECT w.WriteOffID, p.Name AS ProductName, w.Quantity, w.Reason, 
                           w.Description, w.WriteOffDate, w.UserName
                    FROM WriteOffs w
                    JOIN Products p ON w.ProductID = p.ProductID
                    WHERE w.WriteOffDate BETWEEN @StartDate AND @EndDate
                    AND (p.Name LIKE @SearchText OR w.Reason LIKE @SearchText OR w.Description LIKE @SearchText)
                    ORDER BY w.WriteOffDate DESC";

                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate.AddDays(1));
                    command.Parameters.AddWithValue("@SearchText", $"%{searchText}%");

                    var adapter = new SqlDataAdapter(command);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    HistoryDataGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження історії: {ex.Message}");
            }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadWriteOffHistory();
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Today;
            SearchTextBox.Text = "";
            LoadWriteOffHistory();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            LoadWriteOffHistory();
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Звіт_списаних_товарів_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    GeneratePdfReport(saveDialog.FileName);
                    Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при експорті PDF: {ex.Message}");
            }
        }

        private void GeneratePdfReport(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    Document document = new Document(PageSize.A4.Rotate());
                    PdfWriter writer = PdfWriter.GetInstance(document, stream);
                    document.Open();

                    // Шрифти
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    Font titleFont = new Font(baseFont, 18, Font.BOLD);
                    Font headerFont = new Font(baseFont, 12, Font.BOLD);
                    Font normalFont = new Font(baseFont, 10);

                    // Заголовок
                    Paragraph title = new Paragraph("Звіт про списання товарів", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 20;
                    document.Add(title);

                    // Період звіту
                    DateTime startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
                    DateTime endDate = EndDatePicker.SelectedDate ?? DateTime.Today;
                    document.Add(new Paragraph($"Період: з {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}", normalFont));
                    document.Add(new Paragraph($"Дата формування: {DateTime.Now:dd.MM.yyyy HH:mm}", normalFont));
                    document.Add(Chunk.NEWLINE);

                    // Таблиця
                    PdfPTable table = new PdfPTable(6);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 1.5f, 3f, 1f, 1.5f, 3f, 2f });

                    // Заголовки таблиці
                    AddPdfCell(table, "Дата", headerFont);
                    AddPdfCell(table, "Товар", headerFont);
                    AddPdfCell(table, "Кількість", headerFont);
                    AddPdfCell(table, "Причина", headerFont);
                    AddPdfCell(table, "Опис", headerFont);
                    AddPdfCell(table, "Відповідальний", headerFont);

                    // Дані
                    var dataView = HistoryDataGrid.ItemsSource as DataView;
                    if (dataView != null)
                    {
                        foreach (DataRowView row in dataView)
                        {
                            AddPdfCell(table, row["WriteOffDate"].ToString(), normalFont);
                            AddPdfCell(table, row["ProductName"].ToString(), normalFont);
                            AddPdfCell(table, row["Quantity"].ToString(), normalFont);
                            AddPdfCell(table, row["Reason"].ToString(), normalFont);
                            AddPdfCell(table, row["Description"].ToString(), normalFont);
                            AddPdfCell(table, row["UserName"].ToString(), normalFont);
                        }
                    }

                    document.Add(table);
                    document.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при створенні PDF: {ex.Message}");
            }
        }

        private void AddPdfCell(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            table.AddCell(cell);
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    FileName = $"Звіт_списаних_товарів_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var dataView = HistoryDataGrid.ItemsSource as DataView;
                    if (dataView != null && dataView.Table.Rows.Count > 0)
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Списання товарів");

                            // Заголовки
                            for (int i = 0; i < HistoryDataGrid.Columns.Count; i++)
                            {
                                worksheet.Cell(1, i + 1).Value = HistoryDataGrid.Columns[i].Header.ToString();
                            }

                            // Дані
                            for (int row = 0; row < dataView.Count; row++)
                            {
                                for (int col = 0; col < dataView.Table.Columns.Count; col++)
                                {
                                    worksheet.Cell(row + 2, col + 1).Value = dataView[row][col]?.ToString();
                                }
                            }

                            // Спрощене форматування (без XLColor)
                            var headerRange = worksheet.Range(1, 1, 1, dataView.Table.Columns.Count);
                            headerRange.Style.Font.Bold = true;
                            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(211, 211, 211); // Світло-сірий

                            // Автопідбір ширини стовпців
                            worksheet.Columns().AdjustToContents();

                            workbook.SaveAs(saveDialog.FileName);
                        }

                        // Відкрити створений файл
                        Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("Немає даних для експорту");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при експорті Excel: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }
    }
}