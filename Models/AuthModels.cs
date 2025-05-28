using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.Collections.Generic;

namespace WpfApp1.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class Role
    {
        public string Name { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class AuthData
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<Role> Roles { get; set; } = new List<Role>();
    }
}
