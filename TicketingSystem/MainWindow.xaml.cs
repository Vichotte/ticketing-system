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

        public MainWindow() : this(null)
        {
        }
        public MainWindow(WindowStateInfo state = null)
        {
            InitializeComponent();

            _start = new Start.Start();

            if (state != null)
                WindowStateInfo.Apply(this, state);
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _start.ConnectionAsync();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string username = user_text.Text.Trim();
            string password = password_text.Password;

            var result = await _start.ValidateLogin(username, password);

            if (!result.ok)
            {
                MessageBox.Show("Credenciales incorrectas o usuario inactivo.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ✔ Comprobar si es primer login
            DateTime? lastLogin = await _start.GetLastLogin(username);

            if (lastLogin == null)
            {
                // Abrir ventana de cambio de contraseña
                var wnd = new ChangePasswordWindow(username);
                wnd.Show();
                this.Hide();
                return;
            }

            // ✔ Login normal
            await _start.UpdateLastLogin(username);

            int userId = result.userId;
            string displayName = result.displayName;
            int roleId = result.roleId;

            bool isAdmin = (roleId == 1);

            MessageBox.Show("Login Correcto.", "Conexión",
                            MessageBoxButton.OK, MessageBoxImage.Information);

            var state = WindowStateInfo.Capture(this);

            var wnd2 = new first_wndw(isAdmin, displayName, userId, state);
            wnd2.Show();
            this.Hide();
        }



    }
}
