using MindFlayer.saas;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MindFlayer.audio.WavToMp3;

namespace MindFlayer.audio;

public class AudioProcessor : IDisposable
{
    private IWaveIn waveSource;
    private WaveFileWriter waveFile;
    private string outputFolder;
    private int fileCount = 1;
    private int silencePassed;
    private int audioPassed;
    private float silenceThreshold = 0.01f;

    private readonly AudioConverter audioConverter = new();

    
    private const int SilenceDuration = 300;
    private const int AudioDuration = 700;

    public bool IsRecording { get; private set; }

    private Action<Clip> onAudioSegmentProcessed;

    public AudioProcessor(string outputFolderPath = null)
    {
        outputFolder = outputFolderPath ?? Path.Combine(Path.GetTempPath(), "AudioOutput");
        Directory.CreateDirectory(outputFolder);
    }

    public void StartRecording(bool isLoopback = false, Action<Clip> callback = null)
    {
        IsRecording = true;
        onAudioSegmentProcessed = callback;

        if (isLoopback)
        {
            StartLoopbackRecording();
        }
        else
        {
            StartMicrophoneRecording();
        }
    }

    private void StartMicrophoneRecording()
    {
        StartRecording(new WaveInEvent());
    }

    private void StartLoopbackRecording()
    {
        StartRecording(new WasapiLoopbackCapture());        
    }

    private void StartRecording(IWaveIn wave)
    {
        waveSource = wave;
        wave.DataAvailable += OnDataAvailable;
        wave.RecordingStopped += OnRecordingStopped;

        SetupNewWriter();
        wave.StartRecording();
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        ProcessAudioBuffer(e.Buffer, e.BytesRecorded, ((IWaveIn)sender).WaveFormat);
    }

    private void ProcessAudioBuffer(byte[] buffer, int bytesRecorded, WaveFormat waveFormat)
    {
        float max = CalculateMaxAudioLevel(buffer, bytesRecorded);

        Debug.WriteLine($"max: {max}");

        if (max < silenceThreshold)
        {
            HandleSilence(bytesRecorded, waveFormat);
        }
        else
        {
            HandleAudio(buffer, bytesRecorded, waveFormat);
        }
    }

    private float CalculateMaxAudioLevel(byte[] buffer, int bytesRecorded)
    {
        float max = 0f;
        var waveBuffer = new WaveBuffer(buffer);
        for (int index = 0; index < bytesRecorded / 4; index++)
        {
            var sample = Math.Abs(waveBuffer.FloatBuffer[index]);
            if (sample > max) max = sample;
        }
        return max;
    }

    private void HandleSilence(int bytesRecorded, WaveFormat waveFormat)
    {
        silencePassed += (int)((float)bytesRecorded / waveFormat.AverageBytesPerSecond * 1000);
        if (silencePassed >= SilenceDuration && audioPassed > AudioDuration)
        {
            EndCurrentWriterAndStartNewOne();
            silencePassed = 0;
            audioPassed = 0;
        }
    }

    private void HandleAudio(byte[] buffer, int bytesRecorded, WaveFormat waveFormat)
    {
        silencePassed = 0;
        waveFile.Write(buffer, 0, bytesRecorded);
        audioPassed += (int)((float)bytesRecorded / waveFormat.AverageBytesPerSecond * 1000);
    }

    public async void StopRecording()
    {
        StopAndCleanupRecording();

        IsRecording = false;
    }

    private void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
        StopAndCleanupRecording();
    }

    private void StopAndCleanupRecording()
    {
        waveSource?.StopRecording();
        waveSource?.Dispose();
        waveSource = null;

        waveFile?.Dispose();
        waveFile = null;
    }

    private string GetNextFilePath()
    {
        return Path.Combine(outputFolder, $"recorded_{fileCount++}.wav");
    }

    private void SetupNewWriter()
    {
        string outputFilePath = GetNextFilePath();
        waveFile = new WaveFileWriter(outputFilePath, new WasapiLoopbackCapture().WaveFormat);
    }

    private async void EndCurrentWriterAndStartNewOne()
    {
        string wavFilePath = waveFile.Filename;
        string mp3FilePath = Path.ChangeExtension(wavFilePath, ".mp3");

        waveFile?.Flush();
        waveFile?.Dispose();

        var segment = new Clip
        {
            StartTime = DateTime.Now,
            EndTime = DateTime.Now,
            FilePath = mp3FilePath,
            Status = AudioSegmentStatus.Processing
        };

        SetupNewWriter();

        await Task.Run(async () =>
        {
            audioConverter.EncodeWavToMp3(wavFilePath, mp3FilePath);
            segment.Transcription = await TranscribeAudioAsync(mp3FilePath).ConfigureAwait(false);
            segment.Status = AudioSegmentStatus.Completed;

            if (onAudioSegmentProcessed != null)
            {
                onAudioSegmentProcessed(segment);
            }

            if (File.Exists(wavFilePath)) File.Delete(wavFilePath);
            if (File.Exists(mp3FilePath)) File.Delete(mp3FilePath);
        }).ConfigureAwait(false);
    }

    private async Task<string> TranscribeAudioAsync(string audioFilePath)
    {
        return await Task.Run(() => ApiWrapper.Transcribe(audioFilePath)).ConfigureAwait(false);
    }

    public void Dispose()
    {
        StopAndCleanupRecording();
    }
}

public class Clip
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string FilePath { get; set; }
    public string Transcription { get; set; }
    public AudioSegmentStatus Status { get; set; }
}

public enum AudioSegmentStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
