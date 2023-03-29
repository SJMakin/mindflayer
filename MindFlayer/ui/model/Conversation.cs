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
            ChatMessages.Add(new ChatMessage { Role = "system", Content = "You are a helpful assistant." });
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

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ICommand _closeTabCommand;
        public ICommand CloseTabCommand => _closeTabCommand ??= new RelayCommand(() => true, CloseTab);

        private void CloseTab()
        {
            _parent.Conversations.Remove(this);
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
            ChatMessages.Add(new ChatMessage(this)
            {
                Role = "assistant",
                Content = Engine.Chat(ChatMessages, _parent.Temperature)
            });
        }
        
    }
}
