using MySqlConnector;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls; // 🔹 necesario para ComboBoxItem
using System.Windows.Media;

namespace TicketingSystem
{
    public partial class gestion_usuarios : Window
    {
        private WindowStateInfo _previousState;
        private bool _isAdmin;
        private string _displayName;
        private int _userId;
        private Start.Start _start;
        private string _originalUsername;
        private string _selectedUser;


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
            dgUsuarios.SelectionChanged += DgUsuarios_SelectionChanged;

            // 🔙 Volver atrás
            btnBack.Click += (s, e) =>
            {
                var wnd = Application.Current.Windows.OfType<first_wndw>().FirstOrDefault();
                if (wnd != null)
                {
                    wnd.Show();
                    this.Hide();
                }
            };

            // 🚪 Logout
            btnLogout.Click += (s, e) =>
            {
                var login = new MainWindow();
                login.Show();

                foreach (Window w in Application.Current.Windows)
                {
                    if (w != login)
                        w.Hide();
                }

                foreach (Window w in Application.Current.Windows)
                {
                    if (w != login)
                        w.Close();
                }
            };

            btnNotificaciones.Click += (s, e) =>
            {
                notifPanel.Visibility =
                    notifPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            };

            // 🧾 Ver todos los usuarios
            btnVerUsuarios.Click += async (s, e) => await CargarUsuarios();

            // ➕ Crear usuario
            btnCrearUsuario.Click += async (s, e) =>
            {
                string username = txtUsername.Text;
                string displayName = txtDisplayNameForm.Text;
                string role = ((ComboBoxItem)cmbRole.SelectedItem)?.Content.ToString();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(role))
                {
                    MessageBox.Show("Completa todos los campos antes de crear el usuario.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _start.CreateUser(username, displayName, role);
                await CargarUsuarios();
                MessageBox.Show("Usuario creado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            btnEditarUsuario.Click += async (s, e) =>
            {
                if (_originalUsername == null)
                {
                    MessageBox.Show("Selecciona un usuario para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string newUsername = txtUsername.Text;
                string displayName = txtDisplayNameForm.Text;
                string role = ((ComboBoxItem)cmbRole.SelectedItem)?.Content.ToString();

                await _start.UpdateUser(_originalUsername, newUsername, displayName, role);
                await CargarUsuarios();

                MessageBox.Show("Usuario actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                _originalUsername = newUsername; 
            };


            btnEliminarUsuario.Click += async (s, e) =>
            {
                var user = dgUsuarios.SelectedItem as dynamic;

                if (user == null)
                {
                    MessageBox.Show("Selecciona un usuario para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string username = user.username;

                var result = MessageBox.Show(
                    $"¿Seguro que deseas desactivar al usuario '{username}'?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    await _start.DeleteUser(username);
                    await CargarUsuarios();

                    MessageBox.Show("Usuario desactivado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    txtUsername.Text = "";
                    txtDisplayNameForm.Text = "";
                    cmbRole.SelectedIndex = -1;
                }
            };


        }

        private void DgUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var user = dgUsuarios.SelectedItem as dynamic;
            if (user == null)
                return;

            _originalUsername = user.username; // 🔥 Guardamos el original

            txtUsername.Text = user.username;
            txtDisplayNameForm.Text = user.display_name;

            foreach (ComboBoxItem item in cmbRole.Items)
            {
                if (item.Content.ToString().Equals(user.role_name, StringComparison.OrdinalIgnoreCase))
                {
                    cmbRole.SelectedItem = item;
                    break;
                }
            }

            // Ahora SÍ permitimos editar el username
            txtUsername.IsReadOnly = false;
            txtUsername.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }


        private async System.Threading.Tasks.Task CargarUsuarios()
        {
            dgUsuarios.ItemsSource = await _start.GetAllUsers();
        }
    }
}
