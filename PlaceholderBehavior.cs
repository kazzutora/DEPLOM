using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp1
{
    public static class PlaceholderBehavior
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(PlaceholderBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableProperty);
        }

        public static void SetEnable(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableProperty, value);
        }

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotFocus += RemovePlaceholder;
                    textBox.LostFocus += ShowPlaceholder;
                    ShowPlaceholder(textBox, null);
                }
                else
                {
                    textBox.GotFocus -= RemovePlaceholder;
                    textBox.LostFocus -= ShowPlaceholder;
                }
            }
        }

        private static void ShowPlaceholder(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (string.IsNullOrEmpty(textBox.Text) && textBox.Tag != null)
            {
                textBox.Text = textBox.Tag.ToString();
                textBox.Foreground = Brushes.Gray;
            }
        }

        private static void RemovePlaceholder(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox.Tag != null && textBox.Text == textBox.Tag.ToString())
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }
    }
}