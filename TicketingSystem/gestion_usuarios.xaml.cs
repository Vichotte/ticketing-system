using System;
using System.Linq;
using System.Windows;
using MySqlConnector;

namespace TicketingSystem
{
    public partial class gestion_usuarios : Window
    {
        private WindowStateInfo _previousState;
        private bool _isAdmin;
        private string _displayName;
        private int _userId;
        private Start.Start _start;

        public gestion_usuarios(WindowStateInfo previousState, int userId, bool isAdmin, string displayName)
        {
            InitializeComponent();

            _start = new Start.Start();
            _previousState = previousState;
            _userId = userId;
            _isAdmin = isAdmin;
            _displayName = displayName;

            txtDisplayName.Text = displayName;

            WindowStateInfo.Apply(this, previousState);

            // Botón volver atrás
            btnBack.Click += (s, e) =>
            {
                var wnd = new first_wndw(_isAdmin, _displayName, _userId, WindowStateInfo.Capture(this));
                wnd.Show();
                this.Close();
            };

            // Botón logout
            btnLogout.Click += (s, e) =>
            {
                var wnd = new MainWindow(WindowStateInfo.Capture(this));
                wnd.Show();
                this.Close();
            };

            // Crear usuario
            btnCrearUsuario.Click += (s, e) =>
            {
                MessageBox.Show("Aquí abrirías la ventana para crear usuarios.");
            };

            // Ver todos los usuarios
            btnVerUsuarios.Click += async (s, e) =>
            {
                CargarUsuarios();
            };
        }

        private async void CargarUsuarios()
        {
            dgUsuarios.ItemsSource = await _start.GetAllUsers();
        }

    }
}

