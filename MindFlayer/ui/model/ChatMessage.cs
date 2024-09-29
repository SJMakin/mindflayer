using MindFlayer.audio;
using MindFlayer.saas;
using MindFlayer.ui;
using OpenAI.Chat;
using OpenAI.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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

    [JsonInclude]
    [JsonPropertyName("image")]
    public ObservableCollection<string> Images
    {
        get
        {
            return _images;
        }
        set
        {
            _images = value;
            OnPropertyChanged(nameof(Images));
        }
    }

    [JsonIgnore]
    public ObservableCollection<BitmapImage> ImagesAsImageSource => LoadImages();

    private ObservableCollection<BitmapImage> LoadImages()
    {
        var result = new ObservableCollection<BitmapImage>();
        foreach (var base64Image in Images)
        {
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                result.Add(bitmap);
            }
        }
        return result;
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

    public bool IsTextBoxVisible
    {
        get
        {
            return _isTextBoxVisible;
        }
        set
        {

            _isTextBoxVisible = value;
            OnPropertyChanged(nameof(IsTextBoxVisible));
        }
    }

    public Visibility MessageButtonVisibility => Visibility.Visible;  // Role == Role.Assistant ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ChangePromptButtonVisibility => Role == Role.System ? Visibility.Visible : Visibility.Collapsed;

    private ICommand _replayCommand;
    private ICommand _copyCommand;
    private ICommand _readCommand;
    private ICommand _editCommand;
    private ICommand _toggleTextBoxVisibilityCommand;
    private string _content;
    private ObservableCollection<string> _images = new();
    private int _tokenCount;
    private bool _isTextBoxVisible;

    public ICommand ReplayCommand => _replayCommand ??= new RelayCommand(() => true, Replay);
    public ICommand CopyCommand => _copyCommand ??= new RelayCommand(() => true, Copy);
    public ICommand ReadCommand => _readCommand ??= new RelayCommand(() => true, Read);
    public ICommand EditCommand => _editCommand ??= new RelayCommand(() => true, Edit);

    public ICommand ToggleTextBoxVisibilityCommand => _toggleTextBoxVisibilityCommand ??= new RelayCommand(() => true, ToggleTextBoxVisibility);

    private void ToggleTextBoxVisibility()
    {
        IsTextBoxVisible = !IsTextBoxVisible;
    }

    [JsonIgnore]
    public Conversation Parent { get; set; }

    private void Replay()
    {
        if (Role == Role.System)
        {
            // TODO: Open prompt dialog.

            return;
        }

        Parent.ReplayFromThisMessage(this);
    }

    private void Edit()
    {
        var p = new PromptDialog(Content);
        var result = p.ShowDialog();
        if (result.GetValueOrDefault()) Content = p.PromptResult;
    }

    private void Copy()
    {
        Clipboard.SetText(Content);
    }

    private void Read()
    {
        Task.Run(() =>
        {
            var audioData = ApiWrapper.OpenAiClient.AudioEndpoint.CreateSpeechAsync(new OpenAI.Audio.SpeechRequest(Content, Model.TTS_1, OpenAI.Audio.SpeechVoice.Shimmer)).Result;
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
