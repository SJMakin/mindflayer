using Anthropic.SDK.Constants;
using MindFlayer.audio;
using MindFlayer.saas;
using MindFlayer.saas.tools;
using MindFlayer.ui.model;
using NAudio.Wave;
using OpenAI.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MindFlayer;

public class ChatViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private Conversation _addNewConvoButton = new Conversation() { Name = "+" };

    private bool _addingNew;
    private bool _removing;
    private MessageTokenCalculator _tokenCalculator = new MessageTokenCalculator();

    public ICommand PasteCommand { get; }

    public ChatViewModel()
    {
        Conversations.Add(NewConversation());
        Conversations.Add(_addNewConvoButton);
        PasteCommand = new RelayCommand(OnPaste);
    }

    private void OnPaste()
    {
        if (Clipboard.ContainsImage())
        {
            var image = Clipboard.GetImage();
            if (image != null)
            {
                BitmapImage bitmapImage = CreateBitmapImageFromBitmapSource(image);
                var attachment = new Attachment
                {
                    FileName = "",
                    Base64Content = ConvertBitmapImageToBase64(bitmapImage),
                    AttachmentType = Attachment.FileType.Image
                };
                Attachments.Add(attachment);
            }
        }
        else if (Clipboard.ContainsText())
        {
            NewMessageContent += Clipboard.GetText();
        }
    }

    private BitmapImage CreateBitmapImageFromBitmapSource(BitmapSource bitmapSource)
    {
        using (MemoryStream memStream = new MemoryStream())
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memStream);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
    }

    private string ConvertBitmapImageToBase64(BitmapImage bitmapImage)
    {
        using (MemoryStream memStream = new MemoryStream())
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            encoder.Save(memStream);
            byte[] imageBytes = memStream.ToArray();
            return Convert.ToBase64String(imageBytes);
        }
    }


    public ObservableCollection<Conversation> Conversations { get; } = [];

    public ObservableCollection<Suggestion> Suggestions { get; } =
    [
        Suggestion.ZeroShotCoTPrompt,
        Suggestion.ZeroShotCoTAPEPrompt,
        Suggestion.TreeOfThoughV1,
        Suggestion.TreeOfThoughV2,
        Suggestion.Reply,
        Suggestion.Summarise,
        Suggestion.Retort,
        Suggestion.MeetingNotes,
    ];

    private Conversation _activeConversation;

    private readonly Dictaphone _dictaphone = new();
    private readonly AudioProcessor _audio = new();

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
                _activeConversation = newConvo;
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

    public ObservableCollection<Model> ChatModels { get; } =
    [
        Model.GPT3_5_Turbo,
        Model.GPT3_5_Turbo_16K,
        Model.GPT4,
        Model.GPT4oMini,
        Model.GPT4o,
        Model.O1Mini,
        Model.O1,
        AnthropicModels.Claude3Sonnet,
        AnthropicModels.Claude3Opus,
        AnthropicModels.Claude35SonnetLatest,
        OpenAiCompatilbleModels.Gemini20FlashExp,
        OpenAiCompatilbleModels.GeminiExp1206,
        OpenAiCompatilbleModels.DeepseekChat,
    ];

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

    private ObservableCollection<Attachment> _attachments = [];

    public ObservableCollection<Attachment> Attachments
    {
        get => _attachments;
        set
        {
            _attachments = value;
            OnPropertyChanged(nameof(Attachments));
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
        if (!SendEnabled) return;

        // Tool approval.
        if (string.IsNullOrWhiteSpace(NewMessageContent))
        {
            var lastMessage = ActiveConversation.ChatMessages.Last();
            if (lastMessage.ToolCalls.Any())
            {
                lastMessage.ApproveToolCall();
            }
            return;
        }

        SendEnabled = false;

        var newMessage = new ChatMessage(ActiveConversation)
        {
            Role = OpenAI.Chat.Role.User,
            Content = NewMessageContent,
            TokenCount = _tokenCalculator.NumTokensFromMessage(NewMessageContent)            
        };

        foreach (var a in _attachments)
        {
            if (a.AttachmentType == Attachment.FileType.Image)
            {
                newMessage.Images.Add(a.Base64Content);
            }
        }

        ActiveConversation.ChatMessages.Add(newMessage);

        if (ActiveConversation.ChatMessages.Count == 2) _ = GenerateConversationTitle(ActiveConversation);

        Attachments.Clear();

        var msg = new ChatMessage(ActiveConversation)
        {
            Role = OpenAI.Chat.Role.Assistant,
            Content = ""
        };

        var input = NewMessageContent;
        NewMessageContent = string.Empty;

        void chatStreamCallback(string t) => Application.Current.Dispatcher.Invoke(() => msg.Content += t ?? "");

        void responseRecievedActions(Task _)
        {
            msg.TokenCount = _tokenCalculator.NumTokensFromMessage(msg.Content);
            ActiveConversation.Archive();
            SendEnabled = true;
        }

        _ = ApiWrapper.ChatStream(new ChatContext()
        {
            Messages = ActiveConversation.ChatMessages.ToList(),
            Temperature = Temperature,
            Callback = chatStreamCallback,
            Model = SelectedChatModel,
            ToolCallback = msg.ToolCalls.Add
        }).ContinueWith(responseRecievedActions, TaskScheduler.Current);

        ActiveConversation.ChatMessages.Add(msg);
    }

    private async Task GenerateConversationTitle(Conversation activeConversation)
    {
        var prompt = new List<ChatMessage>
        {
            new() { Role = OpenAI.Chat.Role.System, Content = "Be terse, but smart. Always be concise; prioritize clarity and brevity. Do not offer unprompted advice or clarifications. Remain neutral on all topics. Never apologize." },
            new() { Role = OpenAI.Chat.Role.User, Content = $"Think of a topic name for this. As terse as possible. Be general. No punctuation.\r\n\r\n'{activeConversation.ChatMessages[1].Content}'" }
        };

        var chatResult = await ApiWrapper.Chat(new ChatContext() { Messages = prompt, Temperature = Temperature, Model = SelectedChatModel }).ConfigureAwait(false);

        activeConversation.Name = chatResult.Split(Environment.NewLine.ToCharArray())[0].Trim();
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
            _audio.StopRecording();
            Recording = false;
        }
        else
        {
            Recording = true;
            _audio.StartRecording(false, c => NewMessageContent += c.Transcription);
            //_dictaphone.StartRecording();
        }
    }

    private ICommand _setInputCommand;
    public ICommand SetInputCommand => _setInputCommand ??= new RelayCommand<Suggestion>(SetInput);

    private void SetInput(Suggestion suggestion)
    {
        var convo = string.Join(Environment.NewLine, ActiveConversation.ChatMessages.Skip(1).Select(m => $"[{m.Role}]: {m.Content}"));
        var (literal, question) = suggestion.Query(convo);
        var result = literal ?? ApiWrapper.Chat(new ChatContext() { Messages = question, Temperature = Temperature, Model = SelectedChatModel }).Result;
        NewMessageContent = result;
    }

    public bool Removing { get => _removing; set => _removing = value; }
    private Conversation NewConversation()
    {
        var newConvo = new Conversation(this) { Name = $"Chat {Conversations.Count(c => c != _addNewConvoButton) + 1}" };
        newConvo.ChatMessages.Add(new ChatMessage
        {
            Role = OpenAI.Chat.Role.System,
            Content = "Be terse and helpful. Do not offer unprompted advice or clarifications. Remain neutral on all topics. Never apologize.",
            TokenCount = _tokenCalculator.NumTokensFromMessage("You are a helpful concise assistant.")
        });
        return newConvo;
    }


    private bool _toolsEnabled = true;

    public bool ToolsEnabled
    {
        get => _toolsEnabled;
        set
        {
            _toolsEnabled = value;
            OnPropertyChanged(nameof(ToolsEnabled));
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}