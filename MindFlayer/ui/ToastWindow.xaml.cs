using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows.Media.Imaging;

namespace MindFlayer.ui
{
    public partial class ToastWindow : Window
    {
        private DispatcherTimer _timer;
        public string AttachedText { get; private set; }
        public BitmapImage AttachedImage { get; private set; }
        public object ClipboardContent { get; private set; }

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

        public void SetText(string text)
        {
            MessageText.Text = text;
        }

        public void SetText(string text, System.Windows.Media.Brush backColor)
        {
            MessageText.Text = text;
            Border.Background = backColor;
        }

        private void AttachText_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextDialog();
            if (dialog.ShowDialog() == true)
            {
                AttachedText = dialog.EnteredText;
                MessageBox.Show("Text attached successfully!");
            }
        }

        private void AttachImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AttachedImage = new BitmapImage(new Uri(openFileDialog.FileName));
                MessageBox.Show("Image attached successfully!");
            }
        }

        private void AttachClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                ClipboardContent = Clipboard.GetText();
                MessageBox.Show("Text from clipboard attached successfully!");
            }
            else if (Clipboard.ContainsImage())
            {
                ClipboardContent = Clipboard.GetImage();
                MessageBox.Show("Image from clipboard attached successfully!");
            }
            else
            {
                MessageBox.Show("No supported content found in clipboard.");
            }
        }

        public void UpdateThenClose(string text, System.Windows.Media.Brush backColor, int closeAfter)
        {
            SetText(text, backColor);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(closeAfter) };
            _timer.Tick += (s, args) => { _timer.Stop(); Close(); };
            _timer.Start();
        }
    }

    public class TextDialog : Window
    {
        public string EnteredText { get; private set; }

        public TextDialog()
        {
            Width = 300;
            Height = 150;
            Title = "Enter Text";

            var textBox = new TextBox { Margin = new Thickness(10) };
            var button = new Button { Content = "OK", Width = 75, Margin = new Thickness(10) };
            button.Click += (s, e) =>
            {
                EnteredText = textBox.Text;
                DialogResult = true;
            };

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(button);

            Content = stackPanel;
        }
    }
}