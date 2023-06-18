using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using MindFlayer.ui.model;
using NAudio.Wave.Compression;
using NAudio.Wave;

namespace MindFlayer
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Conversation _addNewConvoButton = new Conversation() { Name = "+" };

        private bool _addingNew;
        private bool _removing;
        private MessageTokenCalculator _tokenCalculator = new MessageTokenCalculator();

        public ChatViewModel()
        {
            Conversations.Add(NewConversation());
            Conversations.Add(_addNewConvoButton);
        }

        public ObservableCollection<Conversation> Conversations { get; } = new ObservableCollection<Conversation>();

        public ObservableCollection<Suggestion> Suggestions { get; } = new ObservableCollection<Suggestion>() 
        { 
            new Suggestion() { Summary = "Summerise", Text = "Summary" },
            new Suggestion() { Summary = "Reply", Text = "Reply" },
            new Suggestion() { Summary = "Retort", Text = "Retort" }
        };

        private Conversation _activeConversation;

        private readonly Dictaphone _dictaphone = new();
        private readonly KeyBinder _keyBinder = new();

        public Conversation ActiveConversation
        {
            get => _activeConversation;
            set
            {
                //if (_removing)
                //{  
                //    _removing = false;
                //    ActiveConversation = Conversations.Reverse().Skip(1).First();
                    
                //    return;
                //}
                if (value == _addNewConvoButton && !_addingNew && !_removing)
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
                    _activeConversation = value == _addNewConvoButton ? Conversations.Reverse().Skip(1).First() : value;
                    OnPropertyChanged(nameof(ActiveConversation));
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

        private int _newMessageTokenCount;

        public int NewMessageTokenCount
        {
            get => _newMessageTokenCount;
            set
            {
                _newMessageTokenCount = value;
                OnPropertyChanged(nameof(NewMessageTokenCount));
            }
        }

        private double _temperature = 1.05;

        public double Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                OnPropertyChanged(nameof(Temperature));
            }
        }

        private bool _sendEnabled = true;

        public bool SendEnabled
        {
            get => _sendEnabled;
            set
            {
                _sendEnabled = value;
                OnPropertyChanged(nameof(SendEnabled));
            }
        }

        private ICommand _sendMessageCommand;
        public ICommand SendMessageCommand => _sendMessageCommand ??= new RelayCommand(() => true, SendMessage);

        private void SendMessage()
        {
            ActiveConversation.ChatMessages.Add(new ChatMessage(ActiveConversation)
            {
                Role = OpenAI.Chat.Role.User,
                Content = NewMessageContent,
                TokenCount = _tokenCalculator.NumTokensFromMessage(NewMessageContent)
            });

            SendEnabled = false;

            var msg = new ChatMessage(ActiveConversation)
            {
                Role = OpenAI.Chat.Role.Assistant,
                Content = ""
            };

            ActiveConversation.ChatMessages.Add(msg);

            var input = NewMessageContent;
            NewMessageContent = string.Empty;

            Task.Run(() => Engine.ChatStream(ActiveConversation.ChatMessages, Temperature, (t) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (t.FirstChoice == null) return;
                    msg.Content = msg.Content + t.FirstChoice.Delta.Content;
                    ActiveConversation.TokenCount = t.Usage?.TotalTokens;
                });
            }))
            .ContinueWith(_ => msg.TokenCount = _tokenCalculator.NumTokensFromMessage(msg.Content));
            SendEnabled = true;
        }

        private ICommand _recordInputCommand;
        public ICommand RecordInputCommand => _recordInputCommand ??= new RelayCommand(() => true, RecordInput);

        private bool _recording;
        public bool Recording
        {
            get { return _recording; }
            set
            {
                _recording = value;
                OnPropertyChanged(nameof(Recording));
            }
        }

        private void RecordInput()
        {
            if (Recording)
            {
                NewMessageContent = _dictaphone.StopAndTranscribe();
                Recording = false;
            }
            else
            {
                Recording = true;
                _dictaphone.Record();
            }
        }


        private ICommand _setInputCommand;
        public ICommand SetInputCommand => _setInputCommand ??= new RelayCommand<string>(SetInput);

        private void SetInput(string suggestion)
        {
            var content = string.Join(Environment.NewLine, ActiveConversation.ChatMessages.Skip(1).Select(m => $"[{m.Role}]: {m.Content}"));

            var question = new List<ChatMessage>();

            if (suggestion == "Summary")
            {
                question.Add(new ChatMessage { Role = OpenAI.Chat.Role.System, Content = "You are a helpful assistant, who is an expert at summerisation." });
                question.Add(new ChatMessage { Role = OpenAI.Chat.Role.User, Content = $"Please summerise this conversation down to just the facts:\n{content}" });
            }
            else if (suggestion == "Reply")
            {
                question.Add(new ChatMessage { Role = OpenAI.Chat.Role.System, Content = "You are a chatbot that is a skilled conversationalist, if a little rude/nerdy. You are an expert at getting in to really deep and knowledgeable discussions, and like to debate. You never let a conversation die." });
                question.Add(new ChatMessage { Role = OpenAI.Chat.Role.User, Content = $"Please create a creative user response, that seeks to continue the conversation:\n{content}\n[user]:" });
            }
            else if (suggestion == "Retort")
            {
                question.Add(new ChatMessage { Role = OpenAI.Chat.Role.System, Content = "You are a an extreamly rude and sarcastic chatbot." });
                question.Add(new ChatMessage { Role = OpenAI.Chat.Role.User, Content = $"Please create a creative user response, that seeks to continue the conversation:\n{content}\n[user]:" });
            }

            var result = Engine.Chat(question, Temperature);

            NewMessageContent = result;
        }

        private ICommand _getSuggestionsCommand;
        public ICommand GetSuggestionsCommand => _getSuggestionsCommand ??= new RelayCommand(() => true, GetSuggestions);

        public bool Removing { get => _removing; set => _removing = value; }

        private void GetSuggestions()
        {
            var currectConvo = ActiveConversation.ChatMessages.ToList();
            var question = new List<ChatMessage>();
            question.Add(new ChatMessage(ActiveConversation)
            {
                Role = OpenAI.Chat.Role.User,
                Content = @"Please suggest some continuations for this conversation with the assistant. Write the suggestions in the voice of the user. Pay attention to, and imitate, their style.

Please use the this structured JSON format for your response:

[
    {
        ""summary"": ""Example suggestion"",
        ""text"": ""This is the full text of the suggestion.""
    },   
    {
        ""summary"": ""Another suggestion"",
        ""text"": ""This is the full text of another suggestion.""
    }
]"
            });
            try
            {
                var result = Engine.Chat(question, Temperature);
                var indexOfArrayChar = result.IndexOf("[", StringComparison.Ordinal);
                if (indexOfArrayChar > 0) result = result.Substring(indexOfArrayChar);
                result = result.Replace("```", "");
                var suggestions = JsonSerializer.Deserialize<List<Suggestion>>(result);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Suggestions.Clear();
                    suggestions.ForEach(s => Suggestions.Add(s));
                });
            }
            catch
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Suggestions.Clear();
                    Suggestions.Add(new Suggestion() { Summary = "Failed." });
                });
            }
        }

        private Conversation NewConversation() {
            var newConvo = new Conversation(this) { Name = $"Chat {Conversations.Count(c => c != _addNewConvoButton) + 1}" };
            newConvo.ChatMessages.Add(new ChatMessage { 
                Role = OpenAI.Chat.Role.System, 
                Content = "You are a helpful assistant.",
                TokenCount = _tokenCalculator.NumTokensFromMessage("You are a helpful assistant.")
            });
            return newConvo;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
