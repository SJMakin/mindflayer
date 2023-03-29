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

        private Conversation _addNewConvo = new Conversation() { Name = "+" };
        private bool _addingNew = false;

        public ChatViewModel()
        {
            Conversations.Add(NewConversation());
            Conversations.Add(_addNewConvo);
        }

        public ObservableCollection<Conversation> Conversations { get; } = new ObservableCollection<Conversation>();

        public ObservableCollection<Suggestion> Suggestions { get; } = new ObservableCollection<Suggestion>() { new Suggestion() { Summary = "Loading.." } };

        private Conversation _activeConversation;

        private readonly Dictaphone _dictaphone = new();
        private readonly KeyBinder _keyBinder = new();

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

        private double _temperature = 1.1;

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
                Role = "user",
                Content = NewMessageContent
            });

            SendEnabled = false;

            var input = NewMessageContent;
            NewMessageContent = string.Empty;

            Task.Run(() => Engine.Chat(ActiveConversation.ChatMessages, Temperature))
                .ContinueWith(t =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ActiveConversation.ChatMessages.Add(new ChatMessage(ActiveConversation)
                        {
                            Role = "assistant",
                            Content = t.Result
                        });
                        SendEnabled = true;
                    });
                }).ContinueWith(t => { Task.Run(GetSuggestions); });
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
            NewMessageContent = suggestion;
        }

        private ICommand _getSuggestionsCommand;
        public ICommand GetSuggestionsCommand => _getSuggestionsCommand ??= new RelayCommand(() => true, GetSuggestions);

        private void GetSuggestions()
        {
            var currectConvo = ActiveConversation.ChatMessages.ToList();
            currectConvo.Add(new ChatMessage(ActiveConversation)
            {
                Role = "user",
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
                var result = Engine.Chat(currectConvo, Temperature);
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

        private Conversation NewConversation() => new Conversation(this) { Name = $"Chat {Conversations.Count(c => c != _addNewConvo) + 1}" };

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
