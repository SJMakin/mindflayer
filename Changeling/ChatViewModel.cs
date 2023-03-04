using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MindFlayer;

namespace Changeling
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ChatViewModel()
        {
            Conversations.Add(new Conversation() { Name = "Chat 1"});
        }

        public ObservableCollection<Conversation> Conversations { get; set; } = new ObservableCollection<Conversation>();

        public Conversation ActiveConversation { get; set; }

        private string newMessageContent;

        public string NewMessageContent
        {
            get { return newMessageContent; }
            set
            {
                newMessageContent = value;
                OnPropertyChanged(nameof(NewMessageContent));
            }
        }

        private ICommand sendMessageCommand;

        public ICommand SendMessageCommand
        {
            get
            {
                if (sendMessageCommand == null)
                {
                    sendMessageCommand = new RelayCommand(() => true, SendMessage);
                }

                return sendMessageCommand;
            }
        }

        private ICommand createConversationCommand;

        public ICommand CreateConversationCommand
        {
            get
            {
                if (createConversationCommand == null)
                {
                    createConversationCommand = new RelayCommand(() => true, CreateConversation);
                }

                return createConversationCommand;
            }
        }

        private void SendMessage()
        {
            ActiveConversation.ChatMessages.Add(new ChatMessage
            {
                Role = "user",
                Content = NewMessageContent
            });
            ActiveConversation.ChatMessages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = Engine.Chat(NewMessageContent, ActiveConversation.ChatMessages)
            });
            NewMessageContent = string.Empty;
        }

        private void CreateConversation()
        {
            Conversations.Add(new Conversation() { Name = $"Chat {Conversations.Count + 1}" });
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Conversation : INotifyPropertyChanged
    {
        private ObservableCollection<ChatMessage> chatMessages = new ObservableCollection<ChatMessage>();
        private string name;

        public Conversation()
        {
            ChatMessages.Add(new ChatMessage { Role = "system", Content = "You are a helpful assistant." });
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

    }
}
