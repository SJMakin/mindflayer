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
using System.Windows.Threading;

namespace MindFlayer.ui
{
    /// <summary>
    /// Interaction logic for ToastWindow.xaml
    /// </summary>
    public partial class ToastWindow : Window
    {
        private DispatcherTimer _timer;

        public ToastWindow(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
            Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var workingArea = SystemParameters.WorkArea;
            Left = workingArea.Right - Width;
            Top = workingArea.Bottom - Height;
        }

        public void UpdateThenClose(string message, int closeAfter)
        {
            MessageText.Text = message;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(closeAfter) };
            _timer.Tick += (s, args) => { _timer.Stop(); Close(); };
            _timer.Start();
        }


        public void SetText(string text, System.Windows.Media.Brush backColor)
        {
            MessageText.Text = text;
            Border.Background = backColor;
        }

        public void UpdateThenClose(string text, System.Windows.Media.Brush backColor, int closeAfter)
        {
            SetText(text, backColor);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(closeAfter) };
            _timer.Tick += (s, args) => { _timer.Stop(); Close(); };
            _timer.Start();
        }
    }
}
