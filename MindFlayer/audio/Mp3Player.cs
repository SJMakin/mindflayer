using NAudio.Wave;
using System.IO;

namespace MindFlayer.audio;

internal class Mp3Player
{
    public static void PlayFromMemory(byte[] mp3Bytes)
    {
        using var ms = new MemoryStream(mp3Bytes);
        using var mp3Reader = new Mp3FileReader(ms);
        using var waveOut = new WaveOutEvent();
        waveOut.Init(mp3Reader);
        waveOut.Play();
        while (waveOut.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(100);
        }
    }
}
