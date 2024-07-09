using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MindFlayer.audio;
using MindFlayer.keys;
using MindFlayer.saas;
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
        //HotkeyManager.Current.AddOrReplace(nameof(Replace), Key.LeftCtrl | Key.LeftAlt | Key.G, ReplaceText);
        //HotkeyManager.Current.AddOrReplace(nameof(Pick), Key.LeftCtrl | Key.LeftAlt | Key.P, Pick);
        //HotkeyManager.Current.AddOrReplace(nameof(Record), Key.LeftCtrl | Key.LeftAlt | Key.Y, Record);
        //HotkeyManager.Current.AddOrReplace(nameof(RecordAndDo), Key.LeftCtrl | Key.LeftAlt | Key.R, RecordAndDo);
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

    private void RecordAndDo(object? sender, HotkeyEventArgs e)
    {
        if (_dictaphone.IsRecording)
        {
            var transcription = _dictaphone.StopRecordingAndTranscribe();
            SetTextBasedOnAiResult(transcription);
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
        var (input, acquired) = GetText();
        if (!acquired) return;

        var toast = new ToastWindow("Working...");

        if (SelectedOperation is not { } op) return;

        SetTextBasedOnAiResult(input);
    }

    private void SetTextBasedOnAiResult(string input)
    {

        var toast = new ToastWindow("Working...");

        if (SelectedOperation is not { } op) return;

        try
        {
            var clonedChat = op.Messages.Select(m => new ChatMessage { Role = m.Role, Content = m.Content }).ToList();
            var lastMessage = clonedChat.Last();
            lastMessage.Content = lastMessage.Content.Replace("<{input}>", input, StringComparison.OrdinalIgnoreCase);
            var result = ApiWrapper.Chat(clonedChat, 0.1, Model.GPT4o).Result;
            SetText(result);

            toast.UpdateThenClose("Huzzah!", Brushes.LightGreen, 1500);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            toast.UpdateThenClose("Oh no! Something went wrong.", Brushes.OrangeRed, 3000);
        }
    }


    private static string NormalizeLineEndings(string input) => Regex.Replace(input, @"\r\n|\n\r|\n|\r", "\r\n");

    private (string value, bool acquired) GetText()
    {
        Clipboard.Clear();
        Thread.Sleep(300);
        AdvancedSendKeys.SendKeyDown(KeyCode.CONTROL);
        AdvancedSendKeys.SendKeyPress(KeyCode.KEY_C);
        AdvancedSendKeys.SendKeyUp(KeyCode.CONTROL);
        if (!Clipboard.ContainsText()) return ("", false);
        return (Clipboard.GetText(), true);
    }

    private void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Clipboard.SetText(NormalizeLineEndings(text));
        AdvancedSendKeys.SendKeyDown(KeyCode.CONTROL);
        AdvancedSendKeys.SendKeyPress(KeyCode.KEY_V);
        AdvancedSendKeys.SendKeyUp(KeyCode.CONTROL);
    }
}
