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

namespace WpfApp1
{
    public partial class SalesWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private DataTable salesData;

        public SalesWindow()
        {
            InitializeComponent();
            try
            {
                LoadProducts();
                LoadSalesHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при завантаженні вікна: " + ex.Message);
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
                        SELECT p.ProductID, p.Name, p.Quantity, p.Price, c.CategoryName  
                        FROM Products p
                        JOIN Categories c ON p.CategoryID = c.CategoryID";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    ProductDataGrid.ItemsSource = table.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження товарів: " + ex.Message);
            }
        }

        private void LoadSalesHistory()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    s.SaleID, 
                    p.Name AS ProductName, 
                    s.QuantitySold, 
                    s.TotalAmount,
                    FORMAT(s.SaleDate, 'dd.MM.yyyy HH:mm') AS SaleDate
                FROM Sales s
                JOIN Products p ON s.ProductID = p.ProductID
                ORDER BY s.SaleDate DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    salesData = new DataTable();
                    adapter.Fill(salesData);

                    SalesHistoryDataGrid.AutoGenerateColumns = true;
                    SalesHistoryDataGrid.ItemsSource = salesData.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження історії продажів: " + ex.Message);
            }
        }

        private void BtnSell_Click(object sender, RoutedEventArgs e)
        {
            if (ProductDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Оберіть товар для продажу.");
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
                    MessageBox.Show("Товар відсутній на складі!");
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
                                // 1. Обновляем количество товара
                                SqlCommand updateCmd = new SqlCommand(
                                    "UPDATE Products SET Quantity = Quantity - @QuantitySold WHERE ProductID = @ProductID",
                                    connection, transaction);
                                updateCmd.Parameters.AddWithValue("@QuantitySold", inputDialog.QuantitySold);
                                updateCmd.Parameters.AddWithValue("@ProductID", productId);
                                updateCmd.ExecuteNonQuery();

                                // 2. Добавляем запись о продаже
                                SqlCommand insertCmd = new SqlCommand(
                                    "INSERT INTO Sales (ProductID, QuantitySold, TotalAmount, SaleDate) " +
                                    "VALUES (@ProductID, @QuantitySold, @TotalAmount, GETDATE())",
                                    connection, transaction);

                                insertCmd.Parameters.AddWithValue("@ProductID", productId);
                                insertCmd.Parameters.AddWithValue("@QuantitySold", inputDialog.QuantitySold);
                                insertCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                                insertCmd.ExecuteNonQuery();

                                transaction.Commit();
                                MessageBox.Show($"Продаж {inputDialog.QuantitySold} од. товару '{productName}' на суму {totalAmount:0.00} грн успішний!");

                                // Обновляем данные
                                LoadProducts();
                                LoadSalesHistory();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                MessageBox.Show($"Помилка при продажу: {ex.Message}\n\nДеталі: {ex.StackTrace}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }

        private void BtnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (salesData == null || salesData.Rows.Count == 0)
                {
                    MessageBox.Show("Немає даних для звіту.");
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Звіт про продажі_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    GeneratePdfReport(saveDialog.FileName);
                    MessageBox.Show("Звіт успішно згенеровано!");

                    Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при генерації звіту: " + ex.Message);
            }
        }

        private void GeneratePdfReport(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                Document document = new Document(PageSize.A4, 50, 50, 25, 25);
                PdfWriter writer = PdfWriter.GetInstance(document, stream);

                document.Open();

                // Заголовок
                Paragraph title = new Paragraph("Звіт про продажі",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18));
                title.Alignment = Element.ALIGN_CENTER;
                document.Add(title);

                document.Add(new Paragraph("\n"));

                // Інформація про період
                Paragraph period = new Paragraph($"Період: {DateTime.Now.AddDays(-30):dd.MM.yyyy} - {DateTime.Now:dd.MM.yyyy}",
                    FontFactory.GetFont(FontFactory.HELVETICA, 12));
                document.Add(period);

                document.Add(new Paragraph("\n"));

                // Таблиця з даними
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3, 1, 2, 2 });

                // Заголовки таблиці
                AddCell(table, "Товар", true);
                AddCell(table, "Кількість", true);
                AddCell(table, "Ціна за од.", true);
                AddCell(table, "Сума", true);

                // Дані продажів
                decimal totalRevenue = 0;
                foreach (DataRow row in salesData.Rows)
                {
                    AddCell(table, row["ProductName"].ToString());
                    AddCell(table, row["QuantitySold"].ToString());

                    decimal price = Convert.ToDecimal(row["TotalAmount"]) / Convert.ToInt32(row["QuantitySold"]);
                    AddCell(table, price.ToString("0.00"));

                    AddCell(table, row["TotalAmount"].ToString());
                    totalRevenue += Convert.ToDecimal(row["TotalAmount"]);
                }

                document.Add(table);

                // Підсумок
                document.Add(new Paragraph("\n"));
                Paragraph summary = new Paragraph($"Загальний дохід: {totalRevenue.ToString("0.00")} грн",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14));
                document.Add(summary);

                document.Close();
            }
        }

        private void AddCell(PdfPTable table, string text, bool isHeader = false)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text));
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;

            if (isHeader)
            {
                cell.BackgroundColor = new BaseColor(200, 200, 200);
            }

            table.AddCell(cell);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Введіть пошук...";
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductDataGrid.ItemsSource is DataView dataView)
            {
                string searchText = SearchTextBox.Text.Trim();

                if (string.IsNullOrEmpty(searchText))
                {
                    dataView.RowFilter = "";
                }
                else
                {
                    dataView.RowFilter = $"Name LIKE '%{searchText}%' OR CategoryName LIKE '%{searchText}%'";
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                string searchText = textBox.Text.ToLower();
                if (ProductDataGrid != null && ProductDataGrid.ItemsSource is DataView dataView)
                {
                    dataView.RowFilter = string.IsNullOrWhiteSpace(searchText)
                        ? ""
                        : $"Name LIKE '%{searchText}%' OR CategoryName LIKE '%{searchText}%'";
                }
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Text == "Введіть пошук...")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }
    }
}