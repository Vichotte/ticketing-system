using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TicketingSystem
{
    /// <summary>
    /// Lógica de interacción para ChangePasswordWindow.xaml
    /// </summary>
    public partial class ChangePasswordWindow : Window
    {
        private string _username;
        private Start.Start _start;

        public ChangePasswordWindow(string username)
        {
            InitializeComponent();
            _username = username;
            _start = new Start.Start();
        }

        private async void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string pass1 = txtPass1.Password;
            string pass2 = txtPass2.Password;

            if (pass1 != pass2)
            {
                MessageBox.Show("Las contraseñas no coinciden.");
                return;
            }

            string hash = BCrypt.Net.BCrypt.HashPassword(pass1);

            await _start.UpdatePassword(_username, hash);

            MessageBox.Show("Contraseña actualizada. Inicia sesión de nuevo.");

            var login = new MainWindow();
            login.Show();
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var login = new MainWindow();
            login.Show();
            this.Close();
        }
    }

}
