using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
          
        }

        // Обробник для меню "Файл"
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Додайте тут функціональність для меню "Файл"
        }

        // Обробник для кнопки "Вихід"
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Обробник для меню "Програма"
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            // Додайте функціональність для меню "Програма"
        }

        // Обробник для відкриття продуктів
        private void OpenProducts_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Відкрито розділ Продукти.");
            // Додайте функціональність для відкриття розділу Продукти
            var ProductsWindow = new ProductsWindow();

            // Відкриваємо вікно у модальному режимі
            ProductsWindow.ShowDialog();
        }

        // Обробник для відкриття категорій
        private void OpenCategories_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Відкрито розділ Категорії.");
            var CategoriesWindow = new CategoriesWindow();
            CategoriesWindow.ShowDialog();
         
        }

        // Обробник для відкриття постачальників
        private void OpenSuppliers_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Відкрито розділ Постачальники.");
            var SuppliersWindow = new SuppliersWindow();

            SuppliersWindow.ShowDialog();
        }

        // Обробник для відкриття продажів
        private void OpenSales_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Відкрито розділ Продажі.");
            SalesWindow salesWindow = new SalesWindow();
            salesWindow.Show();
        }

        // Обробник для меню "Про програму"
        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Це програма для управління інвентарем магазину.");
            // Додайте логіку для відкриття інформації про програму
        }
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            // Додайте функціональність для цього меню
            MessageBox.Show("Довідка: Інформація про програму.");
        }
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Головна сторінка");
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Створити новий файл");
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Відкрити файл");
        }
        
    }
    }