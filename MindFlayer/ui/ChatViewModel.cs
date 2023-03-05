using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MindFlayer;

namespace MindFlayer
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Conversation AddNewConvo = new Conversation() { Name = "+" };

        public ChatViewModel()
        {
            Conversations.Add(NewConversation());
            Conversations.Add(AddNewConvo);
        }

        public ObservableCollection<Conversation> Conversations { get; set; } = new ObservableCollection<Conversation>();

        private Conversation activeConversation;
        private bool addingNew = false;
        public Conversation ActiveConversation
        {
            get => activeConversation;
            set
            {
                if (value == AddNewConvo && !addingNew)
                {
                    addingNew = true;
                    var newConvo = NewConversation();
                    Conversations.Insert(Conversations.Count - 1, newConvo);
                    ActiveConversation = newConvo;
                    OnPropertyChanged(nameof(ActiveConversation));
                    addingNew = false;
                }
                else
                {
                    activeConversation = value;
                }
            }
        }

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

        private ICommand recordInputCommand;

        public ICommand RecordInputCommand
        {
            get
            {
                if (recordInputCommand == null)
                {
                    recordInputCommand = new RelayCommand(() => true, RecordInput);
                }

                return recordInputCommand;
            }
        }

        private Conversation NewConversation() => new Conversation(this) { Name = $"Chat {Conversations.Count(c => c != AddNewConvo) + 1}" };

        private void RecordInput()
        {

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
    }
}
