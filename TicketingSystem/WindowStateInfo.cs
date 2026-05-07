using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TicketingSystem;

namespace TicketingSystem
{
    public class WindowStateInfo
    {
        public WindowState State { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }

    public static WindowStateInfo Capture(Window wnd)
        {
            return new WindowStateInfo
            {
                State = wnd.WindowState,
                Width = wnd.Width,
                Height = wnd.Height,
                Left = wnd.Left,
                Top = wnd.Top
            };
        }
        public static void Apply(Window wnd, WindowStateInfo info)
        {
            wnd.WindowStartupLocation = WindowStartupLocation.Manual;

            wnd.WindowState = info.State;

            if (info.State == WindowState.Normal)
            {
                wnd.Width = info.Width;
                wnd.Height = info.Height;
                wnd.Left = info.Left;
                wnd.Top = info.Top;
            }
        }
    }
}

