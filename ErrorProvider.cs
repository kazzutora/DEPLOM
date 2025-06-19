using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    using System.Collections.Generic;

    public class ErrorProvider
    {
        private readonly Dictionary<string, string> _errors = new Dictionary<string, string>();

        public void SetError(string propertyName, string errorMessage)
        {
            _errors[propertyName] = errorMessage;
        }

        public void ClearError(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
            }
        }

        public string GetError(string propertyName)
        {
            return _errors.ContainsKey(propertyName) ? _errors[propertyName] : string.Empty;
        }

        public bool HasErrors => _errors.Count > 0;

        public string GetAllErrors()
        {
            return string.Join("\n", _errors.Values);
        }
    }
}
