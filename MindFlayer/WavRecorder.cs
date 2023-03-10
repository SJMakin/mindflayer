//using System;
//using System.IO;
//using System.Media;
//using System.Windows.Forms;

//namespace MicRecorder
//{
//    class Program
//    {
//        private static WaveIn waveIn;
//        private static MemoryStream memoryStream;
//        private static BinaryWriter binaryWriter;

//        static void Main(string[] args)
//        {
//            Console.WriteLine("Press any key to start recording...");
//            Console.ReadKey(true);

//            waveIn = new WaveIn();

//            // Set format and buffer size
//            waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
//            waveIn.BufferMilliseconds = 100;

//            // Set up event handlers for data received and recording stopped
//            waveIn.DataAvailable += WaveIn_DataAvailable;
//            waveIn.RecordingStopped += WaveIn_RecordingStopped;

//            // Create a memory stream to store the recorded data
//            memoryStream = new MemoryStream();
//            binaryWriter = new BinaryWriter(memoryStream);

//            // Start recording
//            waveIn.StartRecording();

//            Console.WriteLine("Recording. Press any key to stop...");

//            Console.ReadKey(true);

//            // Stop recording
//            waveIn.StopRecording();

//            // Save recorded data to a WAV file
//            SaveToWav("recording.wav", memoryStream.ToArray());

//            Console.WriteLine("Recording saved to recording.wav. Press any key to exit...");
//            Console.ReadKey(true);
//        }

//        private static void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
//        {
//            binaryWriter.Write(e.Buffer, 0, e.BytesRecorded);
//        }

//        private static void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
//        {
//            // Clean up resources
//            waveIn.Dispose();
//            waveIn = null;

//            memoryStream.Dispose();
//            memoryStream = null;

//            binaryWriter.Dispose();
//            binaryWriter = null;
//        }

//        private static void SaveToWav(string path, byte[] data)
//        {
//            using (var fileStream = new FileStream(path, FileMode.Create))
//            {
//                // Write WAV header
//                var writer = new BinaryWriter(fileStream);
//                writer.Write(0x46464952); // "RIFF"
//                writer.Write(data.Length + 36);
//                writer.Write(0x45564157); // "WAVE"
//                writer.Write(0x20746d66); // "fmt "
//                writer.Write(16);
//                writer.Write((short)1); // PCM format
//                writer.Write((short)1); // Mono
//                writer.Write(44100);
//                writer.Write(88200);
//                writer.Write((short)2); // Block align
//                writer.Write((short)16); // Bits per sample
//                writer.Write(0x61746164); // "data"
//                writer.Write(data.Length);
//                writer.Write(data);
//            }
//        }
//    }
//}