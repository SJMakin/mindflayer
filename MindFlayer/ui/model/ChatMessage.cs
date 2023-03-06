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

        private readonly Conversation _parent;

        private ICommand _replayCommand;
        public ICommand ReplayCommand => _replayCommand ??= new RelayCommand(() => true, Replay);

        private void Replay()
        {
            _parent.ReplayFromThisMessage(this);
        }

        public ChatMessage() { }

        public ChatMessage(Conversation parent)
        {
            _parent = parent;
        }
    }
}
