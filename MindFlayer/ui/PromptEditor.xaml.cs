
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer;

public partial class PromptEditor : Window
{
    public ObservableCollection<Header> Headers { get; } = [];

    public PromptEditor()
    {
        InitializeComponent();
    }

    private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void Button_Refresh_Click(object sender, RoutedEventArgs e)
    {
        Headers.Clear();
        Headers.Add(MarkDownParser.Parse(textBoxCrew.Text));
    }

    private void Button_Copy_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(SelectedContent());
    }

    private void Button_Save_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog();
        sfd.Filter = "Metadata files (*.md)|*.md|All files (*.*)|*.*";
        var result = sfd.ShowDialog();

        if (result != true)
            return;

        System.IO.File.WriteAllText(sfd.FileName, SelectedContent());
    }

    private void Button_Load_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog();
        ofd.Filter = "Metadata files (*.md)|*.md|All files (*.*)|*.*"; ;
        var result = ofd.ShowDialog();

        if (result != true)
            return;

        textBoxCrew.Text = System.IO.File.ReadAllText(ofd.FileName);
    }

    private string SelectedContent()
    {
        if (Headers.Count == 0) return string.Empty;
        var content = new StringBuilder();
        foreach (var header in Headers.First().DepthFirst(h => h.Children).Where(h => ItemHelper.GetIsChecked(h).Value))
            content.AppendLine(header?.Content);
        return content.ToString();
    }
}
