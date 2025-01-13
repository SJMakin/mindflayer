using MindFlayer.audio;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace MindFlayer.ui.model;

internal class TranscribeModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    // New properties for prompts management
    public ObservableCollection<string> PromptsCollection { get; set; } = new ObservableCollection<string>();

    private string _newPromptText;
    public string NewPromptText
    {
        get => _newPromptText;
        set
        {
            if (_newPromptText != value)
            {
                _newPromptText = value;
                OnPropertyChanged(nameof(NewPromptText));
            }
        }
    }

    private string _promptExecutionResult;
    public string PromptExecutionResult
    {
        get => _promptExecutionResult;
        set
        {
            if (_promptExecutionResult != value)
            {
                _promptExecutionResult = value;
                OnPropertyChanged(nameof(PromptExecutionResult));
            }
        }
    }

    private string _selectedPrompt;
    public string SelectedPrompt
    {
        get => _selectedPrompt;
        set
        {
            if (_selectedPrompt != value)
            {
                _selectedPrompt = value;
                OnPropertyChanged(nameof(SelectedPrompt));
            }
        }
    }

    public ObservableCollection<AudioSegment> AudioSegments { get; set; } = [];

    public ObservableCollection<AudioSegment> SelectedItems { get; set; } = [];

    public ICommand AddPromptCommand => new RelayCommand(AddPrompt);
    public ICommand RemovePromptCommand => new RelayCommand(RemovePrompt);

    private void AddPrompt()
    {
        if (!string.IsNullOrEmpty(NewPromptText))
        {
            PromptsCollection.Add(NewPromptText);
            NewPromptText = string.Empty;
        }
    }

    private void RemovePrompt()
    {
        if (SelectedPrompt != null)
        {
            PromptsCollection.Remove(SelectedPrompt);
        }
    }

    private RelayCommand startCommand;
    public ICommand StartCommand => startCommand ??= new RelayCommand(Start);

    private CancellationTokenSource cancellationTokenSource;

    private void Start()
    {
        cancellationTokenSource = new CancellationTokenSource();
        _ = AudioCapture.Capture(AudioSegments, cancellationTokenSource.Token);
    }

    private RelayCommand stopCommand;
    public ICommand StopCommand => stopCommand ??= new RelayCommand(Stop);

    private void Stop()
    {
        cancellationTokenSource.Cancel();
    }

    private RelayCommand saveCommand;
    public ICommand SaveCommand => saveCommand ??= new RelayCommand(Save);

    private void Save()
    {
        File.WriteAllText(@$"c:\temp\transription.{DateTime.Now:yyyyMMddhhmmss}.txt", RenderTranscript());
    }

    private RelayCommand copyCommand;
    public ICommand CopyCommand => copyCommand ??= new RelayCommand(Copy);

    private void Copy()
    {
        Clipboard.SetText(RenderTranscript());
    }

    private string RenderTranscript()
    {
        var source = SelectedItems.Any() ? SelectedItems : AudioSegments;
        return string.Join(Environment.NewLine, source.Select(i => i.Transcription));
    }

    private RelayCommand deleteCommand;
    public ICommand DeleteCommand => deleteCommand ??= new RelayCommand(DeleteMethod);

    public void DeleteMethod()
    {
        foreach (var selected in SelectedItems.ToList())
        {
            AudioSegments.Remove(selected);
        }
    }
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
