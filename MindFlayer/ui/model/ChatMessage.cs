using OpenAI.Chat;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer
{
    public class ChatMessage : INotifyPropertyChanged
    {
        [JsonInclude]
        [JsonPropertyName("role")]
        public Role Role { get; set; }

        [JsonInclude]
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

        public int TokenCount
        {
            get
            {
                return _tokenCount;
            }
            set
            {

                _tokenCount = value;
                OnPropertyChanged(nameof(TokenCount));
            }
        }

        public Visibility ReplayButtonVisibility => Role == Role.Assistant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ChangePromptButtonVisibility => Role == Role.System ? Visibility.Visible : Visibility.Collapsed;

        private readonly Conversation _parent;

        private ICommand _replayCommand;
        private string _content;
        private int _tokenCount;

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
