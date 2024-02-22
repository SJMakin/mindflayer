using NAudio.Wave;
using OpenAI.Chat;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace MindFlayer.audio;

public static class AudioCapture
{
    private static WaveFileWriter writer;
    private static string outputFolder;
    private static int fileCount = 1;
    private static readonly float silenceThreshold = 0.01f; // Adjust based on your silence detection needs
    private static readonly int silenceDuration = 500; // milliseconds of silence before splitting
    private static int silencePassed = 0;

    public static async Task Capture(ObservableCollection<AudioSegment> audioSegments, CancellationToken cancellation)
    {
        outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
        Directory.CreateDirectory(outputFolder);
        SetupNewWriter(audioSegments);

        var capture = new WasapiLoopbackCapture();

        capture.DataAvailable += (s, a) =>
        {
            // Calculate the max audio level in this buffer
            float max = 0f;
            var buffer = new WaveBuffer(a.Buffer);
            for (int index = 0; index < a.BytesRecorded / 4; index++)
            {
                var sample = Math.Abs(buffer.FloatBuffer[index]);
                if (sample > max) max = sample;
            }

            // If silence is detected
            if (max < silenceThreshold)
            {
                silencePassed += (int)((float)a.BytesRecorded / capture.WaveFormat.AverageBytesPerSecond * 1000);
                Debug.WriteLine($"Silence {silencePassed}");
                if (silencePassed >= silenceDuration)
                {
                    EndCurrentWriterAndStartNewOne(audioSegments);
                    silencePassed = 0;
                }
            }
            else
            {

                Debug.WriteLine($"Audio");
                silencePassed = 0;
                writer.Write(a.Buffer, 0, a.BytesRecorded);

            }

            // Example condition to stop recording after 20 seconds.
            if (writer.Position > capture.WaveFormat.AverageBytesPerSecond * 90 || cancellation.IsCancellationRequested)
            {
                capture.StopRecording();
            }
        };

        capture.RecordingStopped += (s, a) =>
        {
            writer?.Dispose();
            writer = null;
            capture.Dispose();
        };

        capture.StartRecording();
        while (capture.CaptureState != NAudio.CoreAudioApi.CaptureState.Stopped)
        {
            await Task.Delay(500);
        }
    }

    private static void SetupNewWriter(ObservableCollection<AudioSegment> audioSegments)
    {
        string outputFilePath = Path.Combine(outputFolder, $"recorded_{fileCount}.wav");
        fileCount++;

        var segment = new AudioSegment
        {
            StartTime = DateTime.Now,
            FilePath = outputFilePath,
            Status = TranscriptionStatus.Pending
        };

        App.Current.Dispatcher.Invoke(() =>
        {
            audioSegments.Add(segment);
        });

        writer = new WaveFileWriter(outputFilePath, new WasapiLoopbackCapture().WaveFormat);
    }

    private static void EndCurrentWriterAndStartNewOne(ObservableCollection<AudioSegment> audioSegments)
    {
        string wavFilePath = writer.Filename;
        string mp3FilePath = Path.ChangeExtension(wavFilePath, ".mp3");

        writer?.Dispose();

        var segment = audioSegments.Last();
        segment.EndTime = DateTime.Now;                                         
        segment.Status = TranscriptionStatus.Processing;
        segment.FilePath = mp3FilePath;

        SetupNewWriter(audioSegments);

        try
        {
            // Convert and Transcribe in a background thread (simple example, adjust for real-world use)
            Task.Run(async () =>
            {
                await Task.Delay(500);
                
                new WavToMp3.AudioConverter().EncodeWavToMp3(wavFilePath, mp3FilePath);
                await Task.Delay(500);
                var transcription = Engine.Transcribe(mp3FilePath);

                segment.Transcription = transcription;
                segment.Status = TranscriptionStatus.Completed;
            }).ContinueWith((t) =>
            {
                if (File.Exists(wavFilePath)) File.Delete(wavFilePath);
                if (File.Exists(mp3FilePath)) File.Delete(mp3FilePath);

            }, TaskScheduler.Current);
        }
        catch
        {
            audioSegments.Last().Status = TranscriptionStatus.Failed;
        }
    }

}


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

public enum TranscriptionStatus
{
    Pending,
    Processing,
    Completed,
    Failed // Option to handle errors
}
