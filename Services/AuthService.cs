using System.IO;
using System.Linq;
using System.Text.Json;
using System;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class AuthService
    {
        private const string FilePath = "Data/users.json"; // Перевірте шлях
        private AuthData _authData;

        public AuthService()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FilePath);
                Console.WriteLine($"Шукаємо файл за шляхом: {fullPath}");

                if (File.Exists(fullPath))
                {
                    string json = File.ReadAllText(fullPath);
                    _authData = JsonSerializer.Deserialize<AuthData>(json);
                    Console.WriteLine($"Знайдено {_authData.Users.Count} користувачів");
                }
                else
                {
                    Console.WriteLine("Файл users.json не знайдено!");
                    _authData = new AuthData(); // Створюємо пусті дані
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка завантаження даних: {ex.Message}");
                _authData = new AuthData();
            }
        }

        public User Login(string username, string password)
        {
            try
            {
                // Додамо "хардкодованого" адміна для тесту
                if (username == "admin" && password == "admin")
                {
                    return new User { Username = "admin", Role = "Адміністратор" };
                }

                var user = _authData.Users.FirstOrDefault(u =>
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user != null)
                {
                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    Console.WriteLine($"Перевірка пароля для {username}: {isPasswordValid}");

                    if (isPasswordValid)
                        return user;
                }

                Console.WriteLine($"Користувача {username} не знайдено або пароль невірний");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка входу: {ex.Message}");
                return null;
            }
        }
    }
}