using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WpfApp1
{
    public partial class SuppliersWindow : Window
    {
        private string connectionString = "Server=localhost;Database=StoreInventoryDB;Integrated Security=True;Encrypt=False;";
        private DataView suppliersView;

        public SuppliersWindow()
        {
            InitializeComponent();
            LoadSuppliers();
            var adornerLayer = AdornerLayer.GetAdornerLayer(SearchTextBox);
            if (adornerLayer != null)
            {
                adornerLayer.Add(new PlaceholderAdorner(SearchTextBox, "Пошук постачальника..."));
            }
        }

        // Завантаження постачальників
        private void LoadSuppliers()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT SupplierID, Name, ContactInfo, Category FROM Suppliers";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);

                    DataTable suppliersTable = new DataTable();
                    adapter.Fill(suppliersTable);
                    suppliersView = suppliersTable.DefaultView;
                    SuppliersDataGrid.ItemsSource = suppliersView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при завантаженні даних: {ex.Message}");
            }
        }

        // Додавання нового постачальника
        private void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            AddEditSupplierWindow addSupplierWindow = new AddEditSupplierWindow();
            bool? result = addSupplierWindow.ShowDialog();

            if (result == true)
            {
                LoadSuppliers();
            }
        }

        // Видалення постачальника
        private void DeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (SuppliersDataGrid.SelectedItem is DataRowView selectedRow)
            {
                int supplierID = Convert.ToInt32(selectedRow["SupplierID"]);

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "DELETE FROM Suppliers WHERE SupplierID = @SupplierID";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@SupplierID", supplierID);
                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Постачальника видалено успішно.");
                    LoadSuppliers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при видаленні: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Виберіть постачальника для видалення.");
            }
        }

        // Пошук постачальника
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppliersView != null)
            {
                string filter = SearchTextBox.Text;
                suppliersView.RowFilter = $"Name LIKE '%{filter}%' OR ContactInfo LIKE '%{filter}%' OR Category LIKE '%{filter}%'";
            }
        }
        public void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();

            if (SuppliersDataGrid.ItemsSource is DataView dataView)
            {
                dataView.RowFilter = $"Name LIKE '%{searchText}%' OR ContactInfo LIKE '%{searchText}%' OR Category LIKE '%{searchText}%'";
            }
        }

    }
}
