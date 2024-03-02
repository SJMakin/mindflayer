using System.Diagnostics;
using System.Text.RegularExpressions;
using MindFlayer.audio;
using MindFlayer.ui;
using NHotkey;
using NHotkey.WindowsForms;
using OpenAI.Models;

namespace MindFlayer;

internal class GlobalKeyHooks
{
    private readonly Dictaphone _dictaphone = new();
    OperationPicker picker;

    private static readonly Lazy<GlobalKeyHooks> lazy = new Lazy<GlobalKeyHooks>(() => new GlobalKeyHooks());

    public static GlobalKeyHooks Instance { get { return lazy.Value; } }

    public void Init()
    {
        HotkeyManager.Current.AddOrReplace("Replace", Keys.Control | Keys.Alt | Keys.G, ReplaceText);
        HotkeyManager.Current.AddOrReplace("Pick", Keys.Control | Keys.Alt | Keys.P, Pick);
        HotkeyManager.Current.AddOrReplace("Record", Keys.Control | Keys.Alt | Keys.R, Record);
    }

    private void Record(object? sender, HotkeyEventArgs e)
    {
        if (_dictaphone.IsRecording)
        {
            SetText(_dictaphone.StopRecordingAndTranscribe());
        }
        else
        {
            _dictaphone.StartRecording();
        }
        e.Handled = true;
    }

    private void Pick(object? sender, HotkeyEventArgs e)
    {
        if (picker is not null) return;
        picker = new OperationPicker(SelectedOperation);
        picker.ShowDialog();
        SelectedOperation = picker.SelectedOperation;
        picker = null;
        e.Handled = true;
    }

    private void ReplaceText(object? sender, HotkeyEventArgs e)
    {
        Replace();
        e.Handled = true;
    }

    public Operation SelectedOperation { get; set; }

    private void Replace()
    {
        var input = GetText();
        if (input.acquired) Replace(input.value);
    }

    private void Replace(string input)
    {
        var toast = new Toast("Working...");

        if (SelectedOperation is not { } op) return;

        try
        {
            var clonedChat = op.Messages.Select(m => new ChatMessage { Role = m.Role, Content = m.Content }).ToList();
            var lastMessage = clonedChat.Last();
            lastMessage.Content = lastMessage.Content.Replace("<{input}>", input, StringComparison.OrdinalIgnoreCase);
            var result = Engine.Chat(clonedChat, 0.1, Model.GPT4Preview);
            SetText(result);

            toast.UpdateThenClose("Huzzah!", Color.LightGreen, 1500);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            toast.UpdateThenClose("Oh no! Something went wrong.", Color.OrangeRed, 3000);
        }
    }

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
}
