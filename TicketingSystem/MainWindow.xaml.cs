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
using System.Windows;
using Start;

namespace TicketingSystem
{
    public partial class MainWindow : Window
    {
        private Start.Start _start;
        private WindowStateInfo _previousState;

        // 🔹 Constructor sin parámetros (necesario para WPF)
        public MainWindow() : this(null)
        {
        }

        // 🔹 Constructor principal con estado opcional
        public MainWindow(WindowStateInfo state)
        {
            InitializeComponent();

            _start = new Start.Start();
            _previousState = state;

            if (state != null)
                WindowStateInfo.Apply(this, state);
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

            int? role = await _start.GetUserRole(us_text);
            bool isAdmin = (role == 1);

            string displayName = await _start.GetDisplayName(us_text);

            MessageBox.Show("Login Correcto.", "Conexión", MessageBoxButton.OK, MessageBoxImage.Information);

            var state = WindowStateInfo.Capture(this);
            var wnd = new first_wndw(isAdmin, displayName, state);
            wnd.Show();
            this.Close();
        }
    }
}
