using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WpfApp1
{
    public static class Validator
    {
        public static bool ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return Regex.IsMatch(name, @"^[\p{L}\s'\-]+$");
        }

        public static bool ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            return Regex.IsMatch(phone, @"^[\d\+\-\(\)\s]{7,15}$");
        }

        public static bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static bool ValidateNumber(string number, bool allowDecimal, double min = double.MinValue, double max = double.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(number)) return false;

            if (allowDecimal)
            {
                if (!double.TryParse(number, out double result)) return false;
                return result >= min && result <= max;
            }
            else
            {
                if (!long.TryParse(number, out long result)) return false;
                return result >= min && result <= max;
            }
        }
        public static bool ValidateDate(DateTime? date, DateTime? minDate = null, DateTime? maxDate = null)
        {
            if (!date.HasValue) return false;

            if (minDate.HasValue && date < minDate) return false;
            if (maxDate.HasValue && date > maxDate) return false;

            return true;
        }

        public static bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
            return Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$");
        }

        public static bool ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Regex.IsMatch(url, @"^(https?|ftp):\/\/[^\s/$.?#].[^\s]*$", RegexOptions.IgnoreCase);
        }

        public static bool ValidateProductCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            return Regex.IsMatch(code, @"^[A-Z]{2}-\d{4}$");
        }

        public static bool ValidateZipCode(string zip)
        {
            if (string.IsNullOrWhiteSpace(zip)) return false;
            return Regex.IsMatch(zip, @"^\d{5}(?:[-\s]?\d{4})?$");
        }

        public static bool IsNameCharacter(string text)
        {
            return Regex.IsMatch(text, @"^[\p{L}\s'\-]$");
        }

        public static bool IsPhoneCharacter(string text)
        {
            return Regex.IsMatch(text, @"^[\d\+\-\(\)\s]$");
        }
    }
}
