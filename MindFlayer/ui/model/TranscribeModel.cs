using MindFlayer.audio;
using System.Collections.ObjectModel;

namespace MindFlayer.ui.model
{
    internal class TranscribeModel
    {
        public ObservableCollection<AudioSegment> AudioSegments { get; set; }

    }
}
