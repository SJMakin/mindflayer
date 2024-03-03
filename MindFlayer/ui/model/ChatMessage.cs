using MindFlayer.audio;
using OpenAI.Chat;
using OpenAI.Models;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer;

public class ChatMessage : INotifyPropertyChanged
{
    [JsonInclude]
    [JsonPropertyName("role")]
    public Role Role { get; set; }

    [JsonInclude]
    [JsonPropertyName("content")]
    public string Content
    {
        get
        {
            return _content;
        }
        set
        {
            _content = value;
            OnPropertyChanged(nameof(Content));
        }
    }

    public int TokenCount
    {
        get
        {
            return _tokenCount;
        }
        set
        {

            _tokenCount = value;
            OnPropertyChanged(nameof(TokenCount));
        }
    }

    public Visibility MessageButtonVisibility => Visibility.Visible;  // Role == Role.Assistant ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ChangePromptButtonVisibility => Role == Role.System ? Visibility.Visible : Visibility.Collapsed;

    private ICommand _replayCommand;
    private ICommand _copyCommand;
    private ICommand _readCommand;
    private string _content;
    private int _tokenCount;

    public ICommand ReplayCommand => _replayCommand ??= new RelayCommand(() => true, Replay);
    public ICommand CopyCommand => _copyCommand ??= new RelayCommand(() => true, Copy);
    public ICommand ReadCommand => _readCommand ??= new RelayCommand(() => true, Read);

    [JsonIgnore]
    public Conversation Parent { get; set; }

    private void Replay()
    {
        Parent.ReplayFromThisMessage(this);
    }

    private void Copy()
    {
        System.Windows.Clipboard.SetText(Content);
    }

    private void Read()
    {
        Task.Run(() =>
        {
            var audioData = ModelToOpenAi.Client.AudioEndpoint.CreateSpeechAsync(new OpenAI.Audio.SpeechRequest(Content, Model.TTS_1)).Result;
            Mp3Player.PlayFromMemory(audioData.ToArray());
        });
    }

    public ChatMessage() { }

    public ChatMessage(Conversation parent)
    {
        Parent = parent;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
