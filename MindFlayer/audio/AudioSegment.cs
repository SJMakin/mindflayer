using System.ComponentModel;
using System.Diagnostics;

namespace MindFlayer.audio;

public class AudioSegment : INotifyPropertyChanged
{
    private string transcription;
    private TranscriptionStatus status;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string FilePath { get; set; }
    public string Transcription
    {
        get => transcription; set
        {
            transcription = value;
            OnPropertyChanged(nameof(Transcription));
        }
    }
    public TranscriptionStatus Status
    {
        get => status; set
        {
            status = value;
            OnPropertyChanged(nameof(Status));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
