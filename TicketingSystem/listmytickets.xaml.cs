using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                var wnd = new MainWindow();
                wnd.Show();
                this.Close();
            };

            btnBack.Click += (s, e) =>
            {
                // Volver a la ventana anterior manteniendo el estado
                var wnd = new first_wndw(_isAdmin, _displayName, _userId, _previousState);
                wnd.Show();
                this.Hide();
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
            return border;
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


