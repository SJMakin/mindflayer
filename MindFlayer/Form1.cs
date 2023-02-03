using NHotkey;
using NHotkey.WindowsForms;
using OpenAI;
using OpenAI.Edits;
using OpenAI.Models;

namespace MindFlayer
{
    public partial class Form1 : Form
    {
        // [Environment]::SetEnvironmentVariable('OPENAI_KEY', 'sk-here', 'Machine')
        private OpenAIClient Client = new(OpenAIAuthentication.LoadFromEnv());

        private string Continuation = "";

        public Form1()
        {
            InitializeComponent();
            var operations = System.Text.Json.JsonSerializer.Deserialize<List<Operation>>(File.ReadAllText("operations.json"));
            if (operations == null) throw new Exception();
            comboBox1.Items.AddRange(operations.ToArray());
            comboBox1.DisplayMember = nameof(Operation.Name);
            comboBox1.SelectedIndex = -1;
            HotkeyManager.Current.AddOrReplace("Increment", Keys.Control | Keys.Alt | Keys.G, OnHotKeyPress);
        }

        private void OnHotKeyPress(object sender, HotkeyEventArgs e)
        { 
            Replace();
            e.Handled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Replace();
        }

        private void Replace()
        {
            if (comboBox1.SelectedItem == null) return;

            var op = comboBox1.SelectedItem as Operation;

            var input = Clipboard.GetText();

            Text = "Working...";

            string resultText;
            switch (op.Endpoint)
            {
                case "edit":
                {
                    var request = new EditRequest(input, op.Prompt);
                    var result = Client.EditsEndpoint.CreateEditAsync(request).Result;
                    resultText = result.Choices[0].Text;
                    break;
                }
                case "completion":
                {
                    var result = Client.CompletionsEndpoint.CreateCompletionAsync(
                        op.Prompt.Replace("<{input}>", input),
                        temperature: 0.1,
                        model: Model.Davinci,
                        max_tokens: 256).Result;
                    resultText = result.Completions[0].Text;
                    break;
                }
                default:
                    throw new InvalidOperationException();
            }



            Clipboard.SetText(resultText);

            Text = "Done";
        }
    }
}