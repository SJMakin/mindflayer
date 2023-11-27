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

        public Visibility MessageButtonVisibility => Role == Role.Assistant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ChangePromptButtonVisibility => Role == Role.System ? Visibility.Visible : Visibility.Collapsed;

        private ICommand _replayCommand;
        private ICommand _copyCommand;
        private string _content;
        private int _tokenCount;

        public ICommand ReplayCommand => _replayCommand ??= new RelayCommand(() => true, Replay);
        public ICommand CopyCommand => _copyCommand ??= new RelayCommand(() => true, Copy);

        public Conversation Parent { get; set; }

        private void Replay()
        {
            Parent.ReplayFromThisMessage(this);
        }

        private void Copy()
        {
            System.Windows.Clipboard.SetText(Content);
        }

        public ChatMessage() { }

        public ChatMessage(Conversation parent)
        {
            Parent = parent;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
