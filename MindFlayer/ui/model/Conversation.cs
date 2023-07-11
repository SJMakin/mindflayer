using MindFlayer.ui.model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer
{
    public class Conversation : INotifyPropertyChanged
    {
        private ObservableCollection<ChatMessage> _chatMessages = new ObservableCollection<ChatMessage>();
        private string _name;
        private readonly ChatViewModel _parent;

        public Conversation(ChatViewModel parent)
        {
            this._parent = parent;
            ShowCloseButton = Visibility.Visible;
        }

        public Conversation()
        {
            ShowCloseButton = Visibility.Hidden;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Visibility ShowCloseButton { get; }

        public ObservableCollection<ChatMessage> ChatMessages
        {
            get => _chatMessages;
            set
            {
                _chatMessages = value;
                OnPropertyChanged(nameof(ChatMessages));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public int? TokenCount { get; set; }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ICommand _closeTabCommand;
        public ICommand CloseTabCommand => _closeTabCommand ??= new RelayCommand(() => true, CloseTab);

        private void CloseTab()
        {
            if (_parent.Conversations.Count <= 2) return;
            _parent.Removing = true;
            _parent.Conversations.Remove(this);
            _parent.Removing = false;
        }

        public void ReplayFromThisMessage(ChatMessage message)
        {
            int startIndex = ChatMessages.IndexOf(message);
            if (startIndex >= 0 && startIndex < ChatMessages.Count)
            {
                int count = ChatMessages.Count - startIndex;
                for (int i = 0; i < count; i++)
                {
                    ChatMessages.RemoveAt(startIndex);
                }
            }

            var msg = new ChatMessage(this)
            {
                Role = OpenAI.Chat.Role.Assistant,
                Content = ""
            };

            ChatMessages.Add(msg);

            Task.Run(() => Engine.ChatStream(ChatMessages, _parent.Temperature, (t) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (t.FirstChoice == null) return;
                    msg.Content = msg.Content + t.FirstChoice.Delta.Content;

                });
            }, _parent.SelectedChatModel));
        }
        
    }
}
