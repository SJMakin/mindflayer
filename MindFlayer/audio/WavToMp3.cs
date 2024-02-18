using NAudio.MediaFoundation;
using NAudio.Wave;

namespace MindFlayer.audio
{
    internal class WavToMp3
    {
        public class AudioConverter
        {
            private static object initializationLock = new();
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
                using var reader = new WaveFileReader(wavFilePath);
                MediaFoundationEncoder.EncodeToMp3(reader, mp3FilePath, bitrate);
            }
        }
    }
}
