using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class SalesWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";

        public SalesWindow()
        {
            InitializeComponent();
            try
            {
                if (ProductDataGrid != null) // Перевірка, чи елемент існує
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при завантаженні вікна: " + ex.Message);
            }
        }

        private void LoadData()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT p.ProductID, p.Name, p.Quantity, p.Price, c.CategoryName, p.ImagePath  
                FROM Products p
                JOIN Categories c ON p.CategoryID = c.CategoryID";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    if (ProductDataGrid != null) // Перевірка, чи існує DataGrid
                    {
                        ProductDataGrid.ItemsSource = table.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження товарів: " + ex.Message);
            }
        }


        private void BtnSell_Click(object sender, RoutedEventArgs e)
        {
            if (ProductDataGrid == null || ProductDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Оберіть товар для продажу.");
                return;
            }

            try
            {
                DataRowView row = ProductDataGrid.SelectedItem as DataRowView;
                if (row == null)
                {
                    MessageBox.Show("Помилка отримання даних товару.");
                    return;
                }

                int productId = Convert.ToInt32(row["ProductID"]);
                int quantity = Convert.ToInt32(row["Quantity"]);

                if (quantity <= 0)
                {
                    MessageBox.Show("Товар відсутній на складі!");
                    return;
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        SqlCommand command = new SqlCommand("UPDATE Products SET Quantity = Quantity - 1 WHERE ProductID = @ProductID", connection, transaction);
                        command.Parameters.AddWithValue("@ProductID", productId);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            transaction.Commit();
                            MessageBox.Show("Продаж успішний!");
                            LoadData();
                        }
                        else
                        {
                            transaction.Rollback();
                            MessageBox.Show("Не вдалося оновити товар.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при продажу: " + ex.Message);
            }
        }
        // Метод, який викликається при втраті фокусу текстового поля
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Введіть пошук...";
                textBox.Foreground = Brushes.Gray;
            }
        }

        // Метод, який викликається при зміні тексту в полі пошуку
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                string searchText = textBox.Text.ToLower();
                // Логіка пошуку в DataGrid
                if (ProductDataGrid != null && ProductDataGrid.ItemsSource is DataView dataView)
                {
                    dataView.RowFilter = $"Name LIKE '%{searchText}%'";
                }
            }
        }
        // Метод для очищення текстового поля при фокусуванні
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
