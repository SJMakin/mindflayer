using NAudio.MediaFoundation;
using NAudio.Wave;
using System.IO;

namespace MindFlayer.audio
{
    internal class Dictaphone
    {
        public WaveIn waveSource = null;
        public WaveFileWriter waveFile = null;

        private string file = @"C:\Temp\recording.wav";
        private Toast _toast;
        public void Record()
        {
            Recording = true;
            _toast = new Toast("Recording...");
            waveSource = new WaveIn();
            waveSource.WaveFormat = new WaveFormat(44100, 1);

            waveSource.DataAvailable += waveSource_DataAvailable;
            waveSource.RecordingStopped += waveSource_RecordingStopped;

            if (File.Exists(file)) File.Delete(file);
            waveFile = new WaveFileWriter(file, waveSource.WaveFormat);
            waveSource.StartRecording();

        }

        public static void Capture()
        {
            var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
            Directory.CreateDirectory(outputFolder);
            var outputFilePath = Path.Combine(outputFolder, "recorded.wav");
            var capture = new WasapiLoopbackCapture();
            // optionally we can set the capture waveformat here: e.g. capture.WaveFormat = new WaveFormat(44100, 16,2);
            var writer = new WaveFileWriter(outputFilePath, capture.WaveFormat);



            capture.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);

                // Stop recording after 20 seconds.
                if (writer.Position > capture.WaveFormat.AverageBytesPerSecond * 20)
                {
                    capture.StopRecording();
                }

                // Calculate the current level.
                float max = 0;
                var buffer = new WaveBuffer(a.Buffer);
                // interpret as 32 bit floating point audio
                for (int index = 0; index < a.BytesRecorded / 4; index++)
                {
                    var sample = buffer.FloatBuffer[index];

                    // absolute value 
                    if (sample < 0) sample = -sample;
                    // is this the max value?
                    if (sample > max) max = sample;
                }
            };

            capture.RecordingStopped += (s, a) =>
            {
                writer.Dispose();
                writer = null;
                capture.Dispose();
            };

            capture.StartRecording();
            while (capture.CaptureState != NAudio.CoreAudioApi.CaptureState.Stopped)
            {
                Thread.Sleep(500);
            }
        }

        public static void Encode()
        {
            MediaFoundationApi.Startup();
            var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
            var mp3FilePath = Path.Combine(outputFolder, "test2.mp3");

            var outputFilePath = Path.Combine(outputFolder, "recorded.wav");
            using (var reader = new WaveFileReader(outputFilePath))
            {
                try
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, mp3FilePath, 64000);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void InputBufferToFileCallback(object sender, WaveInEventArgs e)
        {
            // The recorder bytes can be found in e.Buffer
            // The number of bytes recorded can be found in e.BytesRecorded
            // Process the audio data any way you wish...


            if (File.Exists(file)) File.Delete(file);
            waveFile = new WaveFileWriter(file, waveSource.WaveFormat);
            waveSource.StartRecording();
        }

        public string StopAndTranscribe()
        {
            waveSource.StopRecording();
            waveSource.Dispose();
            waveFile.Dispose();
            _toast.UpdateThenClose("Done!", Color.LightGreen, 1500);
            var result = Engine.Transcribe(file);
            File.Delete(file);
            Recording = false;
            return result;
        }

        public bool Recording { get; set; }


        void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();
            }
        }

        void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }

            if (waveFile != null)
            {
                waveFile.Dispose();
                waveFile = null;
            }
        }
    }
}
