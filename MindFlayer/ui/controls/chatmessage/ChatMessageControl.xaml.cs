using System.Windows;
using System.Windows.Input;

namespace MindFlayer.ui.controls.chatmessage;

/// <summary>
/// Interaction logic for ChatMessageControl.xaml
/// </summary>
public partial class ChatMessageControl : System.Windows.Controls.UserControl
{
    public ChatMessageControl()
    {
        InitializeComponent();
    }



    private void MessageContainer_MouseDown(object sender, MouseButtonEventArgs e)
    {
        //MessageContainer.Visibility = Visibility.Collapsed;
        //MarkdownEditor.Visibility = Visibility.Visible;
        //MarkdownEditor.Focus();
    }

    private void MarkdownEditor_LostFocus(object sender, RoutedEventArgs e)
    {
    //    MessageContainer.Visibility = Visibility.Visible;
    //    MarkdownEditor.Visibility = Visibility.Collapsed;
    //    SetMarkdown(MarkdownEditor.Text);
    }
}
