using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NHotkey;
using NHotkey.WindowsForms;

namespace MindFlayer
{
    internal class KeyBinder
    {
        private string _continuation = "";
        private readonly Dictaphone _dictaphone = new();

        public KeyBinder()
        {
            HotkeyManager.Current.AddOrReplace("Replace", Keys.Control | Keys.Alt | Keys.G, ReplaceText);
            HotkeyManager.Current.AddOrReplace("Pick", Keys.Control | Keys.Alt | Keys.P, Pick);
            HotkeyManager.Current.AddOrReplace("Continue", Keys.Control | Keys.Alt | Keys.T, ContinueText);
            HotkeyManager.Current.AddOrReplace("Record", Keys.Control | Keys.Alt | Keys.R, Record);
        }

        private void Record(object? sender, HotkeyEventArgs e)
        {
            if (_dictaphone.Recording)
            {
                SetText(_dictaphone.StopAndTranscribe());
            }
            else
            {
                _dictaphone.Record();
            }
            e.Handled = true;
        }

        private void Pick(object? sender, HotkeyEventArgs e)
        {
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
            { "edit", Engine.Edit },
            { "completion", Engine.Completion },
            //{ "chat", Engine.Chat } // TODO make everything chat based.
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
    }
}
