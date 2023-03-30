using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;

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
    }
}
