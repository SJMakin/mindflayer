using NAudio.MediaFoundation;
using NAudio.Wave;
using System.Diagnostics;
using System.IO;
using System.Windows.Shapes;

namespace MindFlayer.audio
{
    internal class WavToMp3
    {
        public class AudioConverter
        {
            private static object initializationLock = new();
            private static object processLock = new();
            private static bool isInitialized;

            private readonly int bitrate;

            public AudioConverter(int bitrate = 64000)
            {
                InitializeMediaFoundation();
                this.bitrate = bitrate;
            }

            private static void InitializeMediaFoundation()
            {
                lock (initializationLock)
                {
                    if (isInitialized) return;

                    MediaFoundationApi.Startup();
                    isInitialized = true;
                }
            }

            public void EncodeWavToMp3(string wavFilePath, string mp3FilePath)
            {
                lock (processLock)
                {
                    var fileSize = new System.IO.FileInfo(wavFilePath).Length;
                    Debug.WriteLine($"Encoding MP3: {wavFilePath} (Size: {fileSize})");
                    using var reader = new WaveFileReader(wavFilePath);
                    MediaFoundationEncoder.EncodeToMp3(reader, mp3FilePath, bitrate);

                }
            }
        }
    }
}
