using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Anthropic.SDK.Constants;
using MindFlayer.audio;
using MindFlayer.saas;
using MindFlayer.ui.model;
using NAudio.Wave;
using OpenAI.Models;

namespace MindFlayer;

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
        Suggestion.ZeroShotCoTPrompt,
        Suggestion.ZeroShotCoTAPEPrompt,
        Suggestion.TreeOfThoughV1,
        Suggestion.TreeOfThoughV2,
        Suggestion.Reply,
        Suggestion.Summarise,
        Suggestion.Retort,
        Suggestion.MeetingNotes,
    };

    private Conversation _activeConversation;

    private readonly Dictaphone _dictaphone = new();

    public Conversation ActiveConversation
    {
        get => _activeConversation;
        set
        {
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

    public ObservableCollection<Model> ChatModels { get; } = new ObservableCollection<Model>()
    {
        Model.GPT3_5_Turbo,
        Model.GPT3_5_Turbo_16K,
        Model.GPT4,
        Model.GPT4Preview,
        Model.GPT4Turbo,
        Model.GPT4o,
        AnthropicModels.Claude3Sonnet,
        AnthropicModels.Claude3Opus,
        AnthropicModels.Claude35Sonnet,
    };

    private Model _selectedChatModel = Model.GPT4o;

    public Model SelectedChatModel
    {
        get => _selectedChatModel;
        set
        {
            _selectedChatModel = value;
            OnPropertyChanged(nameof(_selectedChatModel));
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

    private double _temperature = 1.00;

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

        if (ActiveConversation.ChatMessages.Count == 2) _ = GenerateConversationTitle(ActiveConversation);

        SendEnabled = false;

        var msg = new ChatMessage(ActiveConversation)
        {
            Role = OpenAI.Chat.Role.Assistant,
            Content = ""
        };

        ActiveConversation.ChatMessages.Add(msg); 

        var input = NewMessageContent;
        NewMessageContent = string.Empty;

        _ = ApiWrapper.ChatStream(ActiveConversation.ChatMessages, Temperature, (t) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (t == null) return;
                msg.Content = msg.Content + t;
            });
        }, SelectedChatModel)
        .ContinueWith(_ => msg.TokenCount = _tokenCalculator.NumTokensFromMessage(msg.Content), TaskScheduler.Current);

        SendEnabled = true;
    }

    private async Task GenerateConversationTitle(Conversation activeConversation)
    {
        var prompt = new List<ChatMessage>
        {
            new ChatMessage { Role = OpenAI.Chat.Role.System, Content = "Be terse, but smart. Always be concise; prioritize clarity and brevity. Do not offer unprompted advice or clarifications. Remain neutral on all topics. Never apologize." },
            
            //"Be terse. Do not offer unprompted advice or clarifications. Remain neutral on all topics. Never apologize." },

            new ChatMessage { Role = OpenAI.Chat.Role.User, Content = $"Think of a topic name for this. As terse as possible. Be general. No punctuation.\r\n\r\n'{activeConversation.ChatMessages[1].Content}'" }
        };
        activeConversation.Name = await ApiWrapper.Chat(prompt, Temperature, SelectedChatModel);
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
            NewMessageContent = _dictaphone.StopRecordingAndTranscribe();
            Recording = false;
        }
        else
        {
            Recording = true;
            _dictaphone.StartRecording();
        }
    }

    private ICommand _setInputCommand;
    public ICommand SetInputCommand => _setInputCommand ??= new RelayCommand<Suggestion>(SetInput);

    private void SetInput(Suggestion suggestion)
    {
        var convo = string.Join(Environment.NewLine, ActiveConversation.ChatMessages.Skip(1).Select(m => $"[{m.Role}]: {m.Content}"));
        var (literal, question) = suggestion.Query(convo);
        var result = literal ?? ApiWrapper.Chat(question, Temperature, SelectedChatModel).Result;
        NewMessageContent = result;
    }

    public bool Removing { get => _removing; set => _removing = value; }
    private Conversation NewConversation()
    {
        var newConvo = new Conversation(this) { Name = $"Chat {Conversations.Count(c => c != _addNewConvoButton) + 1}" };
        newConvo.ChatMessages.Add(new ChatMessage
        {
            Role = OpenAI.Chat.Role.System,
            Content = "Be terse. Do not offer unprompted advice or clarifications. Remain neutral on all topics. Never apologize.",
            TokenCount = _tokenCalculator.NumTokensFromMessage("You are a helpful concise assistant.")
        });
        return newConvo;
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
