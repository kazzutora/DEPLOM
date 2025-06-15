using System.Configuration;
using System.Data;
using System.Windows;
using WpfApp1.Models;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }
    }

}
