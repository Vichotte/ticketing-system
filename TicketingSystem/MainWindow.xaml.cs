using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Start;

namespace TicketingSystem
{
    public partial class MainWindow : Window
    {
        private Start.Start _start;

        public MainWindow()
        {
            InitializeComponent();
            _start = new Start.Start();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _start.ConnectionAsync();
        }
    }
}

