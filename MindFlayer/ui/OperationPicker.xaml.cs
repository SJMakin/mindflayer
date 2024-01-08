using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace MindFlayer.ui
{
    /// <summary>
    /// Interaction logic for OperationPicker.xaml
    /// </summary>
    public partial class OperationPicker : Window
    {
        public List<Operation> Operations { get; set; }
        public Operation SelectedOperation { get; set; }

        public OperationPicker(Operation currentlySelected)
        {
            InitializeComponent();
            JsonSerializerOptions options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
            cboOptions.ItemsSource = JsonSerializer.Deserialize<List<Operation>>(File.ReadAllText("operations.json"), options);
            SelectedOperation = currentlySelected;
            Topmost = true;
        }

        private void OnSelectButtonClicked(object sender, EventArgs e)
        {
            Close();
        }
    }

}
