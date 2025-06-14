﻿using System;
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
    /// <summary>
    /// Interaction logic for EditMinQuantityWindow.xaml
    /// </summary>
    public partial class EditMinQuantityWindow : Window
    {
        public int NewMinQuantity { get; private set; }

        public EditMinQuantityWindow(string productName, int currentMinQuantity)
        {
            InitializeComponent();

            // Встановлюємо заголовок вікна
            Title = $"Редагування: {productName}";

            // Встановлюємо назву товару в інтерфейсі
            ProductNameTextBlock.Text = productName;

            // Встановлюємо поточну кількість
            CurrentMinText.Text = currentMinQuantity.ToString();
            NewMinTextBox.Text = currentMinQuantity.ToString();

            // Фокус на новому полі вводу
            NewMinTextBox.Focus();
            NewMinTextBox.SelectAll();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(NewMinTextBox.Text, out int newMin))
            {
                NewMinQuantity = newMin;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Введіть коректне число!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NewMinTextBox.Focus();
                NewMinTextBox.SelectAll();
            }
        }
    }
}
