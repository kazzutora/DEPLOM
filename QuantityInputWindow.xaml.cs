using System;
using System.Windows;

namespace WpfApp1
{
    public partial class QuantityInputWindow : Window
    {
        public int QuantitySold { get; private set; }
        public decimal TotalAmount { get; private set; }
        private decimal _unitPrice;

        public QuantityInputWindow(string productName, int availableQuantity, decimal unitPrice)
        {
            InitializeComponent();

            Title = $"Продаж товару: {productName}";
            _unitPrice = unitPrice;

            AvailableQuantityText.Text = availableQuantity.ToString();
            QuantitySlider.Minimum = 1;
            QuantitySlider.Maximum = availableQuantity;
            QuantitySlider.Value = 1;
            UnitPriceText.Text = _unitPrice.ToString("0.00");

            UpdateTotalPrice();
        }

        private void UpdateTotalPrice()
        {
            QuantitySold = (int)QuantitySlider.Value;
            TotalAmount = QuantitySold * _unitPrice;
            TotalPriceText.Text = TotalAmount.ToString("0.00");
        }

        private void QuantitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (QuantityText != null)
            {
                QuantityText.Text = ((int)e.NewValue).ToString();
                UpdateTotalPrice();
            }
        }

        private void QuantityText_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(QuantityText.Text, out int quantity))
            {
                if (quantity > QuantitySlider.Maximum)
                {
                    MessageBox.Show($"Недостатня кількість на складі. Максимально доступно: {QuantitySlider.Maximum}",
                                  "Попередження",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }

                quantity = Math.Clamp(quantity, (int)QuantitySlider.Minimum, (int)QuantitySlider.Maximum);

                if (quantity.ToString() != QuantityText.Text)
                {
                    QuantityText.Text = quantity.ToString();
                    QuantityText.CaretIndex = QuantityText.Text.Length;
                }

                QuantitySlider.Value = quantity;
                UpdateTotalPrice();
            }
            else if (!string.IsNullOrEmpty(QuantityText.Text))
            {
                QuantityText.Text = QuantitySlider.Value.ToString();
                QuantityText.CaretIndex = QuantityText.Text.Length;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}