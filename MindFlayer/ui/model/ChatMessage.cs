using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer
{
    public class ChatMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";

        public Visibility ReplayButtonVisibility => Role == "assistant" ? Visibility.Visible : Visibility.Hidden;

        private ICommand replayCommand;
        private readonly Conversation parent;

        public ICommand ReplayCommand
        {
            get
            {
                if (replayCommand == null)
                {
                    replayCommand = new RelayCommand(() => true, Replay);
                }

                return replayCommand;
            }
        }

        private void Replay()
        {
            parent.ReplayFromThisMessage(this);
        }

        public ChatMessage() { }

        public ChatMessage(Conversation parent)
        {
            this.parent = parent;
        }
    }
}
