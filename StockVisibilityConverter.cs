using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1
{
    public class StockVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int quantity && quantity == 0)
                return Visibility.Visible; // Якщо кількість 0 → показуємо "Немає в наявності"

            return Visibility.Collapsed; // Якщо товар є → приховуємо цей текст
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
