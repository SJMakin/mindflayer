using NAudio.Wave.Compression;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace MindFlayer
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
