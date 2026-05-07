using Start;
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
    public partial class first_wndw : Window
    {
        private Start.Start _start;
        private WindowStateInfo _previousState;
        private bool _isAdmin;
        private string _displayName;
        private int _userId;

        public first_wndw(bool isAdmin, string displayName, int userId, WindowStateInfo previousState)
        {
            InitializeComponent();

            _start = new Start.Start();

            _isAdmin = isAdmin;
            _displayName = displayName;
            _userId = userId;
            _previousState = previousState;

            txtDisplayName.Text = displayName;

            if (isAdmin)
            {
            btnTodosTickets.IsEnabled = true;
            btnGestionUsuarios.IsEnabled = true;
            }

            WindowStateInfo.Apply(this, previousState);

            btnCrearTicket.Click += (s, e) =>
            {
                var state = WindowStateInfo.Capture(this);
                var wnd = new create_ticket(state, _userId, _isAdmin, _displayName);
                wnd.Show();
                this.Hide();
            };

            btnMisTickets.Click += (s, e) =>
            {
                var state = WindowStateInfo.Capture(this);
                var wnd = new listmytickets(state, _userId, _isAdmin, _displayName);
                wnd.Show();
                this.Hide();
            };

            btnTodosTickets.Click += (s, e) =>
            {
                var state = WindowStateInfo.Capture(this);
                var wnd = new listalltickets(state, _userId, _isAdmin, _displayName);
                wnd.Show();
                this.Hide();
            };

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

            btnGestionUsuarios.Click += (s, e) =>
            {
                var state = WindowStateInfo.Capture(this);
                var wnd = new gestion_usuarios(state, _userId, _isAdmin, _displayName);
                wnd.Show();
                this.Hide();
            };

        }
    }
}
