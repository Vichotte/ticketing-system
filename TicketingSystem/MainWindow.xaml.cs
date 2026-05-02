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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            
            string us_text = user_text.Text;
            string pa_text = password_text.Password;

            bool ok = await _start.ValidateLogin(us_text, pa_text);

            if (!ok)
            {
                MessageBox.Show("Credenciales incorrectas.");
                return;
            }

            MessageBox.Show("Login correcto.");

            /*
            bool ok = BCrypt.Net.BCrypt.Verify("K2ehmCuxMgvPWm8", "$2a$12$7A1AiVdfKBk8bBfwKXCpqu2STGmIS4HEmRmCVlj1bOReup13sVA0C");
            MessageBox.Show(ok ? "Coincide" : "No coincide");
            */

        }

    }
}

