using MindFlayer.audio;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace MindFlayer.ui.model;

internal class TranscribeModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<AudioSegment> AudioSegments { get; set; } = new();

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
}
