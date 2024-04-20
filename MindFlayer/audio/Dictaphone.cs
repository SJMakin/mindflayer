using MindFlayer.saas;
using NAudio.Wave;
using System.IO;
using static MindFlayer.audio.WavToMp3;

namespace MindFlayer.audio;

internal class Dictaphone
{
    private WaveInEvent waveSource;
    private WaveFileWriter waveFile;
    private readonly string wavFilePath = @"C:\Temp\recording.wav";
    private readonly string mp3FilePath = @"C:\Temp\recording.mp3";
    private Toast toast;
    private readonly AudioConverter audioConverter = new();

    public bool IsRecording { get; private set; }

    public void StartRecording()
    {
        IsRecording = true;
        toast = new Toast("Recording...");
        waveSource = new WaveInEvent
        {
            WaveFormat = new WaveFormat(44100, 1)
        };

        waveSource.DataAvailable += OnDataAvailable;
        waveSource.RecordingStopped += OnRecordingStopped;

        DeleteFileIfExists(wavFilePath);
        waveFile = new WaveFileWriter(wavFilePath, waveSource.WaveFormat);
        waveSource.StartRecording();
    }

    public string StopRecordingAndTranscribe()
    {
        StopAndCleanupRecording();

        toast.SetText("Encoding...", Color.AliceBlue);
        audioConverter.EncodeWavToMp3(wavFilePath, mp3FilePath);

        toast.SetText("Transcribing...", Color.AliceBlue);
        var transcriptionResult = ApiWrapper.Transcribe(mp3FilePath);
        DeleteFileIfExists(wavFilePath);

        toast.UpdateThenClose("Done!", Color.LightGreen, 1500);

        IsRecording = false;
        return transcriptionResult;
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        waveFile.Write(e.Buffer, 0, e.BytesRecorded);
        waveFile.Flush();
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

    private static void DeleteFileIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}