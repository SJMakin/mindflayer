using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer
{
    // ▒ ████▒  ██▓    ▄▄▄     ▓██   ██▓ ▓█████ ██▀███  
    //▒▓██     ▓██▒   ▒████▄    ▒██  ██▒ ▓█   ▀▓██ ▒ ██▒
    //░▒████   ▒██░   ▒██  ▀█▄   ▒██ ██░ ▒███  ▓██ ░▄█ ▒
    //░░▓█▒    ▒██░   ░██▄▄▄▄██  ░ ▐██▓░ ▒▓█  ▄▒██▀▀█▄  
    // ░▒█░   ▒░██████▒▓█   ▓██  ░ ██▒▓░▒░▒████░██▓ ▒██▒
    //  ▒ ░   ░░ ▒░▓  ░▒▒   ▓▒█   ██▒▒▒ ░░░ ▒░ ░ ▒▓ ░▒▓░
    //  ░     ░░ ░ ▒  ░ ░   ▒▒  ▓██ ░▒░ ░ ░ ░    ░▒ ░ ▒ 
    //  ░ ░      ░ ░    ░   ▒   ▒ ▒ ░░      ░    ░░   ░ 
    //        ░    ░        ░   ░ ░     ░   ░     ░     
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppContext.SetSwitch(@"Switch.System.Windows.Controls.DoNotAugmentWordBreakingUsingSpeller", true);
        }

        private void MenuDarkModeButton_Click(object sender, RoutedEventArgs e) => ModifyTheme(DarkModeToggleButton.IsChecked == true);

        private void CloseButtonClickHandler_Click(object sender, RoutedEventArgs e) => Close();

        private void PromptsButtonClickHandler_Click(object sender, RoutedEventArgs e)
        {
            new PromptEditor().ShowDialog();
        }

        private static void ModifyTheme(bool isDarkTheme)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            theme.SetBaseTheme(isDarkTheme ? Theme.Dark : Theme.Light);
            paletteHelper.SetTheme(theme);
        }

        private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private void Sample1_DialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            //Debug.WriteLine($"SAMPLE 1: Closing dialog with parameter: {eventArgs.Parameter ?? string.Empty}");

            //you can cancel the dialog close:
            //eventArgs.Cancel();

            if (!Equals(eventArgs.Parameter, true))
                return;

            //if (!string.IsNullOrWhiteSpace(FruitTextBox.Text))
            //    FruitListBox.Items.Add(FruitTextBox.Text.Trim());
        }
    }
}
