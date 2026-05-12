using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TicketingSystem
{
    public partial class listalltickets : Window
    {
        private WindowStateInfo _previousState;
        private int _userId;
        private bool _isAdmin;
        private string _displayName;
        private Start.Start _start;
        private bool _isInteractingWithCombo = false;



        public listalltickets(WindowStateInfo state, int userId, bool isAdmin, string displayName)
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


            LoadTickets();
        }

        private async void Priority_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cmb) return;
            if (cmb.Tag is not Ticket t) return;

            int newPriority = ((ComboBoxItem)cmb.SelectedItem).Tag switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                _ => t.Priority
            };

            // Actualizar BD
            await _start.UpdateTicketPriority(t.Id, newPriority);

            // Actualizar objeto
            t.Priority = newPriority;

            // Cambiar color del ticket
            var border = FindParentBorder(cmb);
            if (border != null)
            {
                border.Background = newPriority switch
                {
                    1 => (Brush)new BrushConverter().ConvertFrom("#FFEA2E2E"),
                    2 => (Brush)new BrushConverter().ConvertFrom("#FFFCA84A"),
                    3 => (Brush)new BrushConverter().ConvertFrom("#FFF9C74F"),
                    _ => Brushes.White
                };
            }
        }

        private Border FindParentBorder(DependencyObject child)
        {
            while (child != null)
            {
                if (child is Border b)
                    return b;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }


        private async void LoadTickets()
        {
            var tickets = await _start.GetAllTickets();

            foreach (var t in tickets)
            {
                var card = CreateTicketCard(t);

                switch (t.Status.ToLower())
                {
                    case "open":
                        colOpen.Items.Add(card);
                        break;

                    case "in_progress":
                        colProgress.Items.Add(card);
                        break;

                    case "resolved":
                        colResolved.Items.Add(card);
                        break;

                    case "closed":
                        colClosed.Items.Add(card);
                        break;

                    default:
                        colOpen.Items.Add(card);
                        break;
                }
            }
        }


        private Border CreateTicketCard(Ticket t)
        {
            // COLOR SEGÚN PRIORIDAD
            Brush priorityColor = t.Priority switch
            {
                1 => (Brush)new BrushConverter().ConvertFrom("#FFEA2E2E"), // Alta - rojo
                2 => (Brush)new BrushConverter().ConvertFrom("#FFFCA84A"), // Media - naranja
                3 => (Brush)new BrushConverter().ConvertFrom("#FFF9C74F"), // Baja - amarillo
                _ => Brushes.White
            };

            var border = new Border
            {
                Background = priorityColor,
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(10),
                Margin = new Thickness(10),
                Width = 230,
                Tag = t, // GUARDAMOS EL TICKET ENTERO
                Cursor = Cursors.Hand
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = $"#{t.Id} - {t.Title}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            });

            stack.Children.Add(new TextBlock
            {
                Text = t.Description.Length > 60 ? t.Description[..60] + "..." : t.Description,
                FontSize = 14,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 5)
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Usuario: {t.OpenedBy}",
                FontSize = 13,
                Foreground = Brushes.Black
            });

            stack.Children.Add(new TextBlock
            {
                Text = t.CreatedAt,
                FontSize = 12,
                Foreground = Brushes.DarkGray
            });

            // ============================
            // PRIORIDAD (COMBOBOX)
            // ============================
            var priorityPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            priorityPanel.Children.Add(new TextBlock
            {
                Text = "Prioridad:",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 5, 0)
            });

            var cmb = new ComboBox
            {
                Width = 100,
                Height = 25,
                FontSize = 12,
                Tag = t, // Guardamos el ticket
                SelectedIndex = t.Priority - 1
            };

            cmb.Items.Add(new ComboBoxItem { Content = "Alta", Tag = 1 });
            cmb.Items.Add(new ComboBoxItem { Content = "Media", Tag = 2 });
            cmb.Items.Add(new ComboBoxItem { Content = "Baja", Tag = 3 });

            cmb.SelectionChanged += Priority_Changed;
            cmb.PreviewMouseDown += (s, e) => _isInteractingWithCombo = true;
            cmb.DropDownClosed += (s, e) => _isInteractingWithCombo = false;

            priorityPanel.Children.Add(cmb);
            stack.Children.Add(priorityPanel);

            border.Child = stack;

            border.MouseMove += Card_MouseMove;
            border.MouseLeftButtonDown += Ticket_MouseLeftButtonDown;


            return border;
        }

        private DateTime _lastClickTime;

        private void Ticket_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isInteractingWithCombo)
                return;

            var now = DateTime.Now;
            var diff = now - _lastClickTime;
            _lastClickTime = now;

            if (diff.TotalMilliseconds < 300)
            {
                if (sender is Border border && border.Tag is Ticket t)
                {
                    string prioridadTexto = t.Priority switch
                    {
                        1 => "Alta",
                        2 => "Media",
                        3 => "Baja",
                        _ => "Desconocida"
                    };

                    string mensaje =
                        $"📌 TICKET #{t.Id}\n\n" +
                        $"📝 Título:\n{t.Title}\n\n" +
                        $"📊 Prioridad:\n{prioridadTexto}\n\n" +
                        $"📄 Descripción:\n{t.Description}\n\n" +
                        $"👤 Abierto por: {t.OpenedBy}\n" +
                        $"📅 Fecha: {t.CreatedAt}\n";

                    MessageBox.Show(
                        mensaje,
                        "Información del Ticket",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                }
            }
        }




        private void Card_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isInteractingWithCombo)
                return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var card = sender as Border;
                DragDrop.DoDragDrop(card, card, DragDropEffects.Move);
            }
        }



        private void Column_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Border)))
            {
                e.Effects = DragDropEffects.Move;
                ShowDropArea(sender as ListBox);
            }
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void Column_DragLeave(object sender, DragEventArgs e)
        {
            HideDropAreas();
        }



        private void Column_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Border)))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private async void Column_Drop(object sender, DragEventArgs e)
        {
            HideDropAreas();

            if (e.Data.GetData(typeof(Border)) is not Border card)
                return;

            var targetList = sender as ListBox;
            if (targetList == null)
                return;

            // Buscar ListBox origen
            var parentList = FindParentListBox(card);

            // Evitar crash si es null
            if (parentList != null)
                parentList.Items.Remove(card);

            // Evitar duplicados
            if (!targetList.Items.Contains(card))
                targetList.Items.Add(card);

            int ticketId = ((Ticket)card.Tag).Id;

            string newStatus = targetList.Name switch
            {
                "colOpen" => "open",
                "colProgress" => "in_progress",
                "colResolved" => "resolved",
                "colClosed" => "closed",
                _ => "open"
            };

            await _start.UpdateTicketStatus(ticketId, newStatus);
        }

        private void ShowDropArea(ListBox list)
        {
            if (list == colOpen) dropOpen.Visibility = Visibility.Visible;
            if (list == colProgress) dropProgress.Visibility = Visibility.Visible;
            if (list == colResolved) dropResolved.Visibility = Visibility.Visible;
            if (list == colClosed) dropClosed.Visibility = Visibility.Visible;
        }

        private void HideDropAreas()
        {
            dropOpen.Visibility = Visibility.Collapsed;
            dropProgress.Visibility = Visibility.Collapsed;
            dropResolved.Visibility = Visibility.Collapsed;
            dropClosed.Visibility = Visibility.Collapsed;
        }


        private ListBox FindParentListBox(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is ListBox lb)
                    return lb;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }


    }
}


