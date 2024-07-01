using log4net;
using MindFlayer.saas;
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

    private static int silencePassed;
    private static int audioPassed;

    private static readonly ILog Logger = LogManager.GetLogger(typeof(AudioCapture));

    private const float SilenceThreshold = 0.01f; // Adjust based on your silence detection needs
    private const int SilenceDuration = 300; // milliseconds of silence before splitting
    private const int AudioDuration = 700; // milliseconds of silence before splitting

    public static async Task Capture(ObservableCollection<AudioSegment> audioSegments, CancellationToken cancellation)
    {
        outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
        Directory.CreateDirectory(outputFolder);
        SetupNewWriter(audioSegments);

        using var capture = new WasapiLoopbackCapture();

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
            if (max < SilenceThreshold)
            {
                silencePassed += (int)((float)a.BytesRecorded / capture.WaveFormat.AverageBytesPerSecond * 1000);
                Debug.WriteLine($"Silence {silencePassed}");
                //Manage the transition from audio to silence
                if (silencePassed >= SilenceDuration && audioPassed > AudioDuration)
                {
                    EndCurrentWriterAndStartNewOne(audioSegments);
                    silencePassed = 0;
                    audioPassed = 0; // Reset after processing
                }
            }
            else
            {
                Debug.WriteLine($"Audio");
                silencePassed = 0;
                writer.Write(a.Buffer, 0, a.BytesRecorded);
                audioPassed += (int)((float)a.BytesRecorded / capture.WaveFormat.AverageBytesPerSecond * 1000); // Update audio duration
            }

            // Example condition to stop recording after 90 seconds.
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
            await Task.Delay(500, cancellation).ConfigureAwait(false);
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

        writer?.Flush();
        writer?.Dispose();

        var segment = audioSegments.Last();
        segment.EndTime = DateTime.Now;
        segment.Status = TranscriptionStatus.Processing;
        segment.FilePath = mp3FilePath;

        SetupNewWriter(audioSegments);

        Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);

                new WavToMp3.AudioConverter().EncodeWavToMp3(wavFilePath, mp3FilePath);
                await Task.Delay(500).ConfigureAwait(false);
                var transcription = ApiWrapper.Transcribe(mp3FilePath);

                segment.Transcription = transcription;
                segment.Status = TranscriptionStatus.Completed;

                Logger.Info($@"Audio transcribed. transcription=""{segment.Transcription}""");
            }
        ).ContinueWith((t) =>
            {
                if (File.Exists(wavFilePath)) File.Delete(wavFilePath);
                if (File.Exists(mp3FilePath)) File.Delete(mp3FilePath);
                if (t.IsFaulted)
                {
                    segment.Status = TranscriptionStatus.Failed;
                    Logger.Warn($@"Transcription failed. ex=""{t.Exception}""");

                }

            }
        , TaskScheduler.Current);
    }

}
