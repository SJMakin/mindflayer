using NHotkey;
using NHotkey.WindowsForms;
using OpenAI;
using OpenAI.Edits;
using OpenAI.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MindFlayer;

public partial class Main : Form
{
    // [Environment]::SetEnvironmentVariable('OPENAI_KEY', 'sk-here', 'Machine')
    private static readonly OpenAIClient Client = new(OpenAIAuthentication.LoadFromEnv());

    private string _continuation = "";

    public Main()
    {
        var operations = System.Text.Json.JsonSerializer.Deserialize<List<Operation>>(File.ReadAllText("operations.json"));
        if (operations == null) throw new Exception("Unable to parse operations.");
        SetupUi(operations);
        CreateKeyBindings();
    }

    private void SetupUi(List<Operation> operations)
    {
        InitializeComponent();
        comboBox1.Items.AddRange(operations.Cast<object>().ToArray());
        comboBox1.DisplayMember = nameof(Operation.Name);
        comboBox1.SelectedIndex = -1;
    }

    private void CreateKeyBindings()
    {
        HotkeyManager.Current.AddOrReplace("Replace", Keys.Control | Keys.Alt | Keys.G, ReplaceText);
        HotkeyManager.Current.AddOrReplace("Pick", Keys.Control | Keys.Alt | Keys.P, Pick);
        HotkeyManager.Current.AddOrReplace("Continue", Keys.Control | Keys.Alt | Keys.T, ContinueText);
    }

    private void Pick(object? sender, HotkeyEventArgs e)
    {
        BringToFront();
        Activate();
        comboBox1.Focus();
        comboBox1.DroppedDown = true;
        e.Handled = true;
    }

    private void ContinueText(object? sender, HotkeyEventArgs e)
    {
        Replace(_continuation);
        e.Handled = true;
    }

    private void ReplaceText(object? sender, HotkeyEventArgs e)
    {
        Replace();
        e.Handled = true;
    }

    private void Button1_Click(object? sender, EventArgs e)
    {
        Replace();
    }

    private void Replace()
    {
        var input = GetText();
        if (input.acquired) Replace(input.value);
    }

    private void Replace(string input)
    {
        var toast = new Toast("Working...");

        if (comboBox1.SelectedItem is not Operation op) return;

        try
        {
            var result = EndpointActions[op.Endpoint](input, op);
            _continuation = input + result;
            SetText(result);

            toast.UpdateThenClose("Huzzah!", Color.LightGreen, 1500);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            toast.UpdateThenClose("Oh no! Something went wrong.", Color.OrangeRed, 3000);
        }
    }

    private static readonly Dictionary<string, Func<string, Operation, string>> EndpointActions = new()
    {
        { "edit", Edit },
        { "completion", Completion }
    };

    private static string NormalizeLineEndings(string input) => Regex.Replace(input, @"\r\n|\n\r|\n|\r", "\r\n");

    private (string value, bool acquired) GetText()
    {
        Clipboard.Clear();
        Thread.Sleep(300);
        SendKeys.SendWait("^{c}");
        if (!Clipboard.ContainsText()) return ("", false);
        return (Clipboard.GetText(), true);
    }

    private void SetText(string text)
    {
        Clipboard.SetText(NormalizeLineEndings(text));
        SendKeys.SendWait("^{v}");
    }

    private static string Edit(string input, Operation op)
    {
        var request = new EditRequest(input, op.Prompt);
        var result = Client.EditsEndpoint.CreateEditAsync(request).Result;
        return result.Choices[0].Text;
    }

    private static string Completion(string input, Operation op)
    {
        var result = Client.CompletionsEndpoint.CreateCompletionAsync(
                   op.Prompt.Replace("<{input}>", input),
                   temperature: 0.1,
                   model: Model.Davinci,
                   max_tokens: 256).Result;
        return result.Completions[0].Text;
    }
}
