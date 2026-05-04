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
    public partial class create_ticket : Window
    {
        private WindowStateInfo _previousState;
        private Start.Start _start;
        private int _userId;
        private bool _isAdmin;
        private string _displayName;

        public create_ticket(WindowStateInfo state, int userId, bool isAdmin, string displayName)
        {
            InitializeComponent();

            _previousState = state;
            _userId = userId;
            _isAdmin = isAdmin;
            _displayName = displayName;

            WindowStateInfo.Apply(this, state);

            _start = new Start.Start();

            btnCancelar.Click += (s, e) =>
            {
                var currentState = WindowStateInfo.Capture(this);
                var wnd = new first_wndw(_isAdmin, _displayName, _userId, currentState);
                wnd.Show();
                this.Close();
            };

            btnCrear.Click += async (s, e) =>
            {
                string titulo = txtTitulo.Text.Trim();
                string descripcion = txtDescripcion.Text.Trim();

                if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(descripcion))
                {
                    MessageBox.Show("Debes rellenar todos los campos.");
                    return;
                }

                bool ok = await _start.CreateTicket(titulo, descripcion, _userId);

                if (ok)
                {
                    MessageBox.Show("Ticket creado correctamente.");

                    var state = WindowStateInfo.Capture(this);
                    var wnd = new first_wndw(_isAdmin, _displayName, _userId, state);
                    wnd.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Error al crear el ticket.");
                }
            };

        }
    }
}


