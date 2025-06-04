using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Mail;
using System.IO;
using static WpfApp1.OrderInputWindow;

namespace WpfApp1
{
    public partial class ShoppingCartWindow : Window
    {
        public ShoppingCartWindow()
        {
            InitializeComponent();
            CartDataGrid.ItemsSource = ShoppingCart.Instance.Items;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShoppingCart.Instance.Items.Count == 0)
            {
                MessageBox.Show("Кошик порожній", "Попередження");
                return;
            }

            // Групуємо товари за постачальниками
            var ordersBySupplier = ShoppingCart.Instance.Items
                .GroupBy(item => item.SupplierEmail)
                .ToList();

            bool allEmailsSent = true;
            string errorMessages = "";

            foreach (var supplierGroup in ordersBySupplier)
            {
                string supplierEmail = supplierGroup.Key;
                if (string.IsNullOrEmpty(supplierEmail))
                {
                    errorMessages += $"Для товару '{supplierGroup.First().ProductName}' не вказано email постачальника.\n";
                    allEmailsSent = false;
                    continue;
                }

                try
                {
                    SendOrderEmailToSupplier(supplierGroup.ToList(), supplierEmail);
                }
                catch (Exception ex)
                {
                    errorMessages += $"Помилка відправки email до {supplierEmail}: {ex.Message}\n";
                    allEmailsSent = false;
                }
            }

            if (!allEmailsSent)
            {
                MessageBox.Show($"Деякі листи не вдалося відправити:\n{errorMessages}", "Попередження");
            }
            else
            {
                MessageBox.Show("Всі замовлення успішно відправлені постачальникам", "Успіх");
            }

            DialogResult = true;
            Close();
        }

        private void SendOrderEmailToSupplier(List<CartItem> items, string supplierEmail)
        {
            string tempPdfPath = Path.Combine(Path.GetTempPath(), $"Order_{Guid.NewGuid()}.pdf");
            string supplierName = items.First().SupplierName;

            try
            {
                // Генеруємо PDF
                OrderPdfGenerator.GenerateOrderEmailPdf(
                    tempPdfPath,
                    items,
                    supplierName,
                    DateTime.Now,
                    items.First().ExpectedDate
                );

                // Налаштування SMTP для Gmail
                using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("diabloroale9@gmail.com", "mlfb nkqk mdfa qjjr"); // Використовуйте App Password

                    // Додаткова перевірка перед відправкою
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.Timeout = 10000; // 10 секунд

                    // Створення листа
                    using (MailMessage message = new MailMessage())
                    {
                        message.From = new MailAddress("your_email@gmail.com");
                        message.To.Add(supplierEmail);
                        message.Subject = $"Замовлення товарів від {DateTime.Now:dd.MM.yyyy}";
                        message.Body = $"Шановний постачальник {supplierName},\n\n" +
                                     $"Додано нове замовлення {items.Count} товарів.\n" +
                                     $"Загальна сума: {items.Sum(i => i.TotalPrice):N2} грн\n\n" +
                                     $"Деталі у додатку.\n\nЗ повагою,\nВаш магазин";
                        message.Attachments.Add(new Attachment(tempPdfPath));

                        // Відправка з обробкою помилок
                        try
                        {
                            client.Send(message);
                        }
                        catch (SmtpException smtpEx)
                        {
                            throw new Exception($"SMTP помилка: {smtpEx.StatusCode}. {smtpEx.Message}");
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(tempPdfPath))
                {
                    try { File.Delete(tempPdfPath); } catch { }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (CartDataGrid.SelectedItem is CartItem selectedItem)
            {
                ShoppingCart.Instance.RemoveItem(selectedItem);
                CartDataGrid.Items.Refresh();
            }
        }
    }
}