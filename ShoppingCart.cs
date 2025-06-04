using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WpfApp1.OrderInputWindow;

namespace WpfApp1
{
    public sealed class ShoppingCart
    {
        private static readonly ShoppingCart instance = new ShoppingCart();
        private readonly List<CartItem> items = new List<CartItem>();

        // Статичний конструктор гарантує, що ініціалізація відбувається лише один раз
        static ShoppingCart() { }

        // Приватний конструктор запобігає створенню екземплярів ззовні
        private ShoppingCart() { }

        // Публічна властивість для доступу до єдиного екземпляра
        public static ShoppingCart Instance => instance;

        // Публічна властивість для отримання списку товарів (тільки для читання)
        public IReadOnlyList<CartItem> Items => items.AsReadOnly();

        // Метод для додавання товару до кошика
        public void AddItem(CartItem item)
        {
            items.Add(item);
        }

        // Метод для очищення кошика
        public void Clear()
        {
            items.Clear();
        }

        // Метод для видалення конкретного товару з кошика
        public void RemoveItem(CartItem item)
        {
            items.Remove(item);
        }
    }
}
