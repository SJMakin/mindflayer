using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
            ActiveConversation.ChatMessages.Add(new ChatMessage(ActiveConversation)
            {
                Role = "user",
                Content = NewMessageContent
            });

            ActiveConversation.ChatMessages.Add(new ChatMessage(ActiveConversation)
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
}
