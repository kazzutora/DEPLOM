using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class AddEditSupplierWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private readonly ErrorProvider errorProvider = new ErrorProvider();
        public AddEditSupplierWindow()
        {
            InitializeComponent();
            LoadCategories(); // Завантаження категорій при відкритті
            SetupValidation();
        }
        private void SetupValidation()
        {
            NameTextBox.TextChanged += (s, e) => ValidateName();
            ContactInfoTextBox.TextChanged += (s, e) => ValidateContactInfo();
            EmailTextBox.TextChanged += (s, e) => ValidateEmail();
            CategoryComboBox.SelectionChanged += (s, e) => ValidateCategory();
        }

        private void ValidateName()
        {
            string name = NameTextBox.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorProvider.SetError("Name", "Назва обов'язкова");
                NameTextBox.BorderBrush = Brushes.Red;
            }
            else if (name.Length > 25)
            {
                errorProvider.SetError("Name", "Максимум 25 символів");
                NameTextBox.BorderBrush = Brushes.Red;
            }
            else if (!Regex.IsMatch(name, @"^[\p{L}\s'\-\.]+$"))
            {
                errorProvider.SetError("Name", "Тільки літери, пробіли, апострофи та дефіси");
                NameTextBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Name");
                NameTextBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
        }

        private void ValidateContactInfo()
        {
            string contact = ContactInfoTextBox.Text;

            if (string.IsNullOrWhiteSpace(contact))
            {
                errorProvider.SetError("Contact", "Контакт обов'язковий");
                ContactInfoTextBox.BorderBrush = Brushes.Red;
            }
            else if (contact.Length > 15)
            {
                errorProvider.SetError("Contact", "Максимум 15 символів");
                ContactInfoTextBox.BorderBrush = Brushes.Red;
            }
            else if (!Regex.IsMatch(contact, @"^[\p{L}\d\s\-\+\(\)\.\,\:]+$"))
            {
                errorProvider.SetError("Contact", "Недопустимі символи");
                ContactInfoTextBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Contact");
                ContactInfoTextBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
        }

        private void ValidateEmail()
        {
            string email = EmailTextBox.Text;

            if (string.IsNullOrWhiteSpace(email))
            {
                errorProvider.SetError("Email", "Email обов'язковий");
                EmailTextBox.BorderBrush = Brushes.Red;
            }
            else if (email.Length > 30)
            {
                errorProvider.SetError("Email", "Максимум 30 символів");
                EmailTextBox.BorderBrush = Brushes.Red;
            }
            else if (!Validator.ValidateEmail(email))
            {
                errorProvider.SetError("Email", "Невірний формат email");
                EmailTextBox.BorderBrush = Brushes.Red;
            }
            else if (!email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                errorProvider.SetError("Email", "Тільки Gmail адреси");
                EmailTextBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Email");
                EmailTextBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
        }

        private void ValidateCategory()
        {
            if (CategoryComboBox.SelectedItem == null)
            {
                errorProvider.SetError("Category", "Оберіть категорію");
                CategoryComboBox.BorderBrush = Brushes.Red;
            }
            else
            {
                errorProvider.ClearError("Category");
                CategoryComboBox.BorderBrush = SystemColors.ControlDarkBrush;
            }
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
            ValidateName();
            ValidateContactInfo();
            ValidateEmail();
            ValidateCategory();

            if (errorProvider.HasErrors)
            {
                MessageBox.Show($"Виправте помилки:\n{errorProvider.GetAllErrors()}");
                return;
            }

            string name = NameTextBox.Text;
            string contactInfo = ContactInfoTextBox.Text;
            string category = CategoryComboBox.SelectedItem as string;
            string email = EmailTextBox.Text;

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