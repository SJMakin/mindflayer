
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer
{
    /// <summary>
    /// Interaction logic for PromptEditor.xaml
    /// </summary>
    public partial class PromptEditor : Window
    {
        public ObservableCollection<Header> Headers { get; } = new ObservableCollection<Header>();

        public PromptEditor()
        {
            InitializeComponent();


        }

        private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Button_PrintCrew_Click(object sender, RoutedEventArgs e)
        {
            Headers.Clear();

            var headers = MarkDownParser.Parse(textBoxCrew.Text).Children;

            foreach (var header in MarkDownParser.Parse(textBoxCrew.Text).Children)
                Headers.Add(header as Header);
        }
    }
}
