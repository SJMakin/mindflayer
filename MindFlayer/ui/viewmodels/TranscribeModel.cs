using MindFlayer.audio;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace MindFlayer.ui.model;

internal class TranscribeModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<AudioSegment> AudioSegments { get; set; } = new();

    public ObservableCollection<AudioSegment> SelectedItems { get; set; } = new();

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
}
