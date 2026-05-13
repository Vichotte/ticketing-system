using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TicketingSystem
{
    public partial class listmytickets : Window
    {
        private WindowStateInfo _previousState;
        private Start.Start _start;
        private int _userId;
        private bool _isAdmin;
        private string _displayName;

        public listmytickets(WindowStateInfo state, int userId, bool isAdmin, string displayName)
        {
            InitializeComponent();

            _previousState = state;
            _userId = userId;
            _isAdmin = isAdmin;
            _displayName = displayName;

            WindowStateInfo.Apply(this, state);

            _start = new Start.Start();

            txtDisplayName.Text = _displayName;

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

            btnBack.Click += (s, e) =>
            {
                var wnd = Application.Current.Windows
                 .OfType<first_wndw>()
                 .FirstOrDefault();

                if (wnd != null)
                {
                    wnd.Show();
                    this.Hide();
                }
            };

            btnNotificaciones.Click += (s, e) =>
            {
                notifPanel.Visibility =
                    notifPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            };

            LoadTickets();
        }

        private async void LoadTickets()
        {
            var tickets = await GetTicketsByUser(_userId);

            foreach (var t in tickets)
                wpTickets.Children.Add(CreateTicketCard(t));
        }

        private Border CreateTicketCard(Ticket t)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 249, 196)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(15),
                Margin = new Thickness(10),
                Width = 240,
                Height = 160,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 10,
                    ShadowDepth = 2,
                    Opacity = 0.3
                }
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = t.Title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Estado: {t.Status}",
                FontSize = 14,
                Margin = new Thickness(0, 5, 0, 0),
                Foreground = Brushes.DarkSlateGray
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Prioridad: {t.Priority}",
                FontSize = 14,
                Foreground = Brushes.DarkSlateGray
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Creado: {t.CreatedAt}",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 0),
                Foreground = Brushes.Gray
            });

            stack.Children.Add(new TextBlock
            {
                Text = t.Description.Length > 80
           ? t.Description.Substring(0, 80) + "..."
           : t.Description,
                FontSize = 14,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 5)
            });

            border.Child = stack;
            border.MouseLeftButtonDown += Ticket_MouseLeftButtonDown;
            border.Tag = t;
            return border;
        }

        private DateTime _lastClickTime;

        private void Ticket_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var now = DateTime.Now;
            var diff = now - _lastClickTime;
            _lastClickTime = now;

            if (diff.TotalMilliseconds < 300)
            {
                if (sender is Border border && border.Tag is Ticket t)
                {
                    ShowTicketDetails(t);
                }
            }
        }


        private void ShowTicketDetails(Ticket t)
        {
            // Ventana
            var wnd = new Window
            {
                Title = $"Ticket #{t.Id} - Detalles",
                Width = 560,
                Height = 520,
                Background = new SolidColorBrush(Color.FromRgb(255, 249, 196)),
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize
            };

            // Root grid con 3 filas: contenido, spacer, footer (botón)
            var root = new Grid { Margin = new Thickness(18) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // contenido (scroll)
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // spacer
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // footer (botón)
            wnd.Content = root;

            // ScrollViewer para evitar recortes en pantallas pequeñas
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(scroll, 0);
            root.Children.Add(scroll);

            var contentStack = new StackPanel { Orientation = Orientation.Vertical };
            scroll.Content = contentStack;

            // Título editable
            contentStack.Children.Add(new TextBlock
            {
                Text = "📝 Título:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var txtTitle = new TextBox
            {
                Text = t.Title ?? string.Empty,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 12),
                IsReadOnly = !_isAdmin && t.OpenedBy != _displayName
            };
            contentStack.Children.Add(txtTitle);

            // Descripción editable
            contentStack.Children.Add(new TextBlock
            {
                Text = "📄 Descripción:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var txtDesc = new TextBox
            {
                Text = t.Description ?? string.Empty,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Height = 220,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 12),
                IsReadOnly = !_isAdmin && t.OpenedBy != _displayName
            };
            contentStack.Children.Add(txtDesc);

            // Info extra
            contentStack.Children.Add(new TextBlock
            {
                Text = $"📊 Estado: {t.Status}\n🔥 Prioridad: {t.Priority}\n👤 Usuario: {t.OpenedBy}\n📅 Creado: {t.CreatedAt}",
                FontSize = 14,
                Foreground = Brushes.DarkSlateGray,
                Margin = new Thickness(0, 6, 0, 6)
            });

            // FOOTER: botón dentro de Grid row 2
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(footerGrid, 2);
            root.Children.Add(footerGrid);

            // Reusable SolidColorBrush para animar
            var borderBrush = new SolidColorBrush(Color.FromRgb(252, 122, 0));

            // BOTÓN GUARDAR (estilo Tomodachi)
            var btnSaveBorder = new Border
            {
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(Color.FromRgb(252, 122, 0)), // naranja base
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 25, 0, 25),
                Padding = new Thickness(0),
                Height = 50,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 8,
                    ShadowDepth = 2,
                    Opacity = 0.25
                },
                Visibility = (!_isAdmin && t.OpenedBy != _displayName) ? Visibility.Collapsed : Visibility.Visible
            };

            var btnSave = new Button
            {
                Content = "Guardar cambios",
                Width = 180,
                Height = 40,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            // Hover igual que tus otros botones
            btnSave.MouseEnter += (s, e) =>
            {
                btnSaveBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFCA84A")); // naranja más claro
                btnSaveBorder.Opacity = 0.9;
            };
            btnSave.MouseLeave += (s, e) =>
            {
                btnSaveBorder.Background = new SolidColorBrush(Color.FromRgb(252, 122, 0)); // color base
                btnSaveBorder.Opacity = 1;
            };

            // Acción guardar (sin deshabilitar)
            btnSave.Click += async (s, e) =>
            {
                t.Title = txtTitle.Text;
                t.Description = txtDesc.Text;

                try
                {
                    await _start.UpdateTicketInfo(t.Id, t.Title, t.Description);
                    MessageBox.Show("Ticket actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al actualizar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                wnd.Close();

                // REFRESCAR: limpia y recarga
                wpTickets.Children.Clear();
                LoadTickets();
            };

            btnSaveBorder.Child = btnSave;
            Grid.SetColumn(btnSaveBorder, 1);
            footerGrid.Children.Add(btnSaveBorder);

            // Mostrar modal
            wnd.ShowDialog();

        }




        private async Task<List<Ticket>> GetTicketsByUser(int userId)
        {
            var list = new List<Ticket>();

            try
            {
                using var conn = new MySqlConnection(_start.GetConnectionString());
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = "get_tickets_by_user";
                cmd.Parameters.AddWithValue("@p_user_id", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new Ticket
                    {
                        Id = reader.GetInt32("id"),
                        Title = reader.GetString("title"),
                        Description = reader.GetString("description"),
                        Status = reader.GetString("status"),
                        Priority = reader.GetInt32("priority"),
                        CreatedAt = reader.GetDateTime("created_at").ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar tickets: " + ex.Message);
            }

            return list;
        }
    }

    public class Ticket
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int Priority { get; set; }
        public string CreatedAt { get; set; }
        public string OpenedBy { get; set; }
    }

}


