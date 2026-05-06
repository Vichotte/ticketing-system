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
                var wnd = new MainWindow();
                wnd.Show();
                this.Close();
            };

            btnBack.Click += (s, e) =>
            {
                // Volver a la ventana anterior manteniendo el estado
                var wnd = new first_wndw(_isAdmin, _displayName, _userId, _previousState);
                wnd.Show();
                this.Close();
            };


            LoadTickets();
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
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(10),
                Margin = new Thickness(10),
                Width = 230,
                Tag = t.Id,
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

            border.Child = stack;

            border.MouseMove += Card_MouseMove;

            return border;
        }

        private void Card_MouseMove(object sender, MouseEventArgs e)
        {
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

            int ticketId = (int)card.Tag;

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


