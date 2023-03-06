using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer
{
    public class Conversation : INotifyPropertyChanged
    {
        private ObservableCollection<ChatMessage> chatMessages = new ObservableCollection<ChatMessage>();
        private string name;
        private readonly ChatViewModel parent;

        public Conversation(ChatViewModel parent)
        {
            this.parent = parent;
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
            get { return chatMessages; }
            set
            {
                chatMessages = value;
                OnPropertyChanged(nameof(ChatMessages));
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ICommand closeTabCommand;

        public ICommand CloseTabCommand
        {
            get
            {
                if (closeTabCommand == null)
                {
                    closeTabCommand = new RelayCommand(() => true, CloseTab);
                }

                return closeTabCommand;
            }
        }

        private void CloseTab()
        {
            parent.Conversations.Remove(this);
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
                Content = Engine.Chat(ChatMessages)
            });
        }
    }
}
