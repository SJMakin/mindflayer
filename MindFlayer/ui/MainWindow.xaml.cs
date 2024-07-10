using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MindFlayer;

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

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        GlobalKeyHooks.Instance.Init();
    }

    private void MenuDarkModeButton_Click(object sender, RoutedEventArgs e) => ModifyTheme(DarkModeToggleButton.IsChecked == true);

    private void CloseButtonClickHandler_Click(object sender, RoutedEventArgs e) => Close();

    private void PromptsButtonClickHandler_Click(object sender, RoutedEventArgs e) => new PromptEditor().Show();

    private void ImagesButtonClickHandler_Click(object sender, RoutedEventArgs e)
    {
        var im = new ui.ImageViewer();
        im.Show();
        im.Activate();
    }

    private void TranscribeButtonClickHandler_Click(object sender, RoutedEventArgs e) => new ui.Transcribe().Show();

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
            DragMove();
    }

    private void Settings_DialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventArgs)
    {
        //you can cancel the dialog close:
        //eventArgs.Cancel();

        if (!Equals(eventArgs.Parameter, true))
            return;
    }

    private void FlowDocument_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var viewModel = (ChatMessage)DataContext;
        if (viewModel.ToggleTextBoxVisibilityCommand.CanExecute(null))
            viewModel.ToggleTextBoxVisibilityCommand.Execute(null);
    }
    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var viewModel = (ChatMessage)DataContext;
        if (viewModel.ToggleTextBoxVisibilityCommand.CanExecute(null))
            viewModel.ToggleTextBoxVisibilityCommand.Execute(null);
    }

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //if (e.Source is System.Windows.Controls.TabControl)
        //{
        //    Debug.WriteLine("Setting focus.");
        //    InputBox.Focus();
        //}
    }
}
