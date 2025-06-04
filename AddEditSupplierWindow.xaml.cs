using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class AddEditSupplierWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";

        public AddEditSupplierWindow()
        {
            InitializeComponent();
            LoadCategories(); // Завантаження категорій при відкритті
        }

        private void LoadCategories()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT DISTINCT CategoryName FROM Categories";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    List<string> categories = new List<string>();

                    while (reader.Read())
                    {
                        categories.Add(reader["CategoryName"].ToString());
                    }

                    CategoryComboBox.ItemsSource = categories;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження категорій: {ex.Message}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text;
            string contactInfo = ContactInfoTextBox.Text;
            string category = CategoryComboBox.SelectedItem as string;
            string email = EmailTextBox.Text;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(contactInfo) || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Заповніть всі поля.");
                return;
            }

            // Перевірка на наявність @ в email
            if (!email.Contains("@"))
            {
                MessageBox.Show("Email повинен містити символ '@'");
                EmailTextBox.Focus();
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO Suppliers (Name, ContactInfo, Category, Email) VALUES (@Name, @ContactInfo, @Category, @Email)";
                    SqlCommand command = new SqlCommand(query, connection);

                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@ContactInfo", contactInfo);
                    command.Parameters.AddWithValue("@Category", category);
                    command.Parameters.AddWithValue("@Email", email);

                    command.ExecuteNonQuery();
                }

                MessageBox.Show("Постачальника додано успішно.");
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при додаванні: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}