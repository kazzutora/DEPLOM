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
using static WpfApp1.OrderInputWindow;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for ShoppingCartWindow.xaml
    /// </summary>
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

            DialogResult = true;
            Close();
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
