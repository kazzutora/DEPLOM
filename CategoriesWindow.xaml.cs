using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace WpfApp1
{
    public partial class CategoriesWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";

        public CategoriesWindow()
        {
            InitializeComponent();
            LoadCategories();
        }

        // Завантаження категорій з бази даних
        private void LoadCategories()
        {
            CategoriesListBox.Items.Clear();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT CategoryID, CategoryName FROM Categories";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        CategoriesListBox.Items.Add(new Category
                        {
                            CategoryID = reader.GetInt32(0),
                            CategoryName = reader.GetString(1)
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Помилка завантаження категорій: {ex.Message}");
            }
        }

        // Додавання нової категорії
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Введіть назву категорії:", "Додати категорію", "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "INSERT INTO Categories (CategoryName) VALUES (@CategoryName)";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@CategoryName", input);
                        command.ExecuteNonQuery();
                    }
                    LoadCategories();
                    MessageBox.Show("Категорію додано успішно.");
                }
                catch (SqlException ex)
                {
                    MessageBox.Show($"Помилка додавання категорії: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Назва категорії не може бути пустою.");
            }
        }

        // Видалення категорії
        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesListBox.SelectedItem is Category selectedCategory)
            {
                if (MessageBox.Show($"Ви впевнені, що хочете видалити '{selectedCategory.CategoryName}'?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = "DELETE FROM Categories WHERE CategoryID = @CategoryID";
                            SqlCommand command = new SqlCommand(query, connection);
                            command.Parameters.AddWithValue("@CategoryID", selectedCategory.CategoryID);
                            command.ExecuteNonQuery();
                        }
                        LoadCategories();
                        MessageBox.Show("Категорію видалено успішно.");
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show($"Помилка видалення категорії: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, виберіть категорію для видалення.");
            }
        }

        // Закриття вікна
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // Клас для зберігання категорій
    public class Category
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
    }
}