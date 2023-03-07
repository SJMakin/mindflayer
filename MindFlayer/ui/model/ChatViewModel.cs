using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Input;
using MindFlayer.ui.model;

namespace MindFlayer
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Conversation _addNewConvo = new Conversation() { Name = "+" };
        private bool _addingNew = false;

        public ChatViewModel()
        {
            Conversations.Add(NewConversation());
            Conversations.Add(_addNewConvo);
        }

        public ObservableCollection<Conversation> Conversations { get; } = new ObservableCollection<Conversation>();

        public ObservableCollection<Suggestion> Suggestions { get; } = new ObservableCollection<Suggestion>() {new Suggestion() {Summary = "Loading.."}};

        private Conversation _activeConversation;
        
        public Conversation ActiveConversation
        {
            get => _activeConversation;
            set
            {
                if (value == _addNewConvo && !_addingNew)
                {
                    _addingNew = true;
                    var newConvo = NewConversation();
                    Conversations.Insert(Conversations.Count - 1, newConvo);
                    ActiveConversation = newConvo;
                    OnPropertyChanged(nameof(ActiveConversation));
                    _addingNew = false;
                }
                else
                {
                    _activeConversation = value;
                }
            }
        }

        private string _newMessageContent;

        public string NewMessageContent
        {
            get => _newMessageContent;
            set
            {
                _newMessageContent = value;
                OnPropertyChanged(nameof(NewMessageContent));
            }
        }

        private ICommand _sendMessageCommand;
        public ICommand SendMessageCommand => _sendMessageCommand ??= new RelayCommand(() => true, SendMessage);

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

            Task.Run(GetSuggestions);
        }
        
        private ICommand _recordInputCommand;
        public ICommand RecordInputCommand => _recordInputCommand ??= new RelayCommand(() => true, RecordInput);
        
        private void RecordInput()
        {

        }


        private ICommand _getSuggestionsCommand;
        public ICommand GetSuggestionsCommand => _getSuggestionsCommand ??= new RelayCommand(() => true, GetSuggestions);

        private void GetSuggestions()
        {
            var currectConvo = ActiveConversation.ChatMessages.ToList();
            currectConvo.Add(new ChatMessage(ActiveConversation)
            {
                Role = "user",
                Content = @"Please provide some suggestions for how the user may continue this conversation with the assistant. Write the suggestions in the voice of the user.

Use the this format for your response - provide valid json ONLY:
[
    {
        summary: ""Example suggestion""
        text: ""This is the full text of the suggestion.""
    }
]"
            });
            var result = Engine.Chat(currectConvo);
            var suggestions = JsonSerializer.Deserialize<List<Suggestion>>(result);
            System.Windows.Application.Current.Dispatcher.Invoke(
                () => {
                    Suggestions.Clear();
                    suggestions.ForEach(s => Suggestions.Add(s));
                });
        }

        private Conversation NewConversation() => new Conversation(this) { Name = $"Chat {Conversations.Count(c => c != _addNewConvo) + 1}" };

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
