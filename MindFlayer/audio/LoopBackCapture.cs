using NAudio.Wave;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace MindFlayer.audio;

public static class AudioCapture
{
    private static WaveFileWriter writer;
    private static string outputFolder;
    private static int fileCount = 1;
    private static readonly float silenceThreshold = 0.01f; // Adjust based on your silence detection needs
    private static readonly int silenceDuration = 500; // milliseconds of silence before splitting
    private static int silencePassed = 0;

    public static void Capture()
    {
        outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
        Directory.CreateDirectory(outputFolder);
        SetupNewWriter();

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
                    EndCurrentWriterAndStartNewOne();
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
            if (writer.Position > capture.WaveFormat.AverageBytesPerSecond * 90)
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
            Thread.Sleep(500);
        }
    }

    private static void SetupNewWriter()
    {
        string outputFilePath = Path.Combine(outputFolder, $"recorded_{fileCount}.wav");
        fileCount++;

        var segment = new AudioSegment
        {
            StartTime = DateTime.Now,
            FilePath = outputFilePath,
            Status = TranscriptionStatus.Pending
        };
        // Consider invoking on the UI thread if necessary
        AudioSegments.Add(segment);

        writer = new WaveFileWriter(outputFilePath, new WasapiLoopbackCapture().WaveFormat);
    }

    private static void EndCurrentWriterAndStartNewOne()
    {
        writer?.Dispose();
        AudioSegments.Last().EndTime = DateTime.Now; // Assumes this method is called in sequence

        string wavFilePath = writer.Filename;
        string mp3FilePath = Path.ChangeExtension(wavFilePath, ".mp3");

        // Convert WAV to MP3
        new WavToMp3.AudioConverter().EncodeWavToMp3(wavFilePath, mp3FilePath);

        // Delete the WAV, its metadata already updated
        File.Delete(wavFilePath);

        // Mark the segment as processing
        AudioSegments.Last().Status = TranscriptionStatus.Processing;

        SetupNewWriter();

        // Transcribe in a background thread (simple example, adjust for real-world use)
        Task.Run(() =>
        {
            var transcription = Engine.Transcribe(mp3FilePath);
            var segment = AudioSegments.FirstOrDefault(s => s.FilePath == mp3FilePath);
            if (segment != null)
            {
                segment.Transcription = transcription;
                segment.Status = TranscriptionStatus.Completed;
            }
        });
    }

    public static ObservableCollection<AudioSegment> AudioSegments = new ObservableCollection<AudioSegment>();

}


public class AudioSegment
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string FilePath { get; set; }
    public string Transcription { get; set; }
    public TranscriptionStatus Status { get; set; }
}

public enum TranscriptionStatus
{
    Pending,
    Processing,
    Completed,
    Failed // Option to handle errors
}
