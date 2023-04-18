using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer
{
    public class ChatMessage : INotifyPropertyChanged
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("content")]
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {

                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        public Visibility ReplayButtonVisibility => Role == "assistant" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ChangePromptButtonVisibility => Role == "system" ? Visibility.Visible : Visibility.Collapsed;

        private readonly Conversation _parent;

        private ICommand _replayCommand;
        private string _content;

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
