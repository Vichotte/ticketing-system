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
    /// Lógica de interacción para first_wndw.xaml
    /// </summary>
    public partial class first_wndw : Window
    {
        public first_wndw(bool isAdmin, string displayName, WindowStateInfo previousState)
        {
            InitializeComponent();

            txtDisplayName.Text = displayName;

            if (isAdmin)
                btnTodosTickets.IsEnabled = true;

            WindowStateInfo.Apply(this, previousState);

            btnLogout.Click += (s, e) =>
            {
                var login = new MainWindow(previousState);
                login.Show();
                this.Close();
            };
        }
    }
}
