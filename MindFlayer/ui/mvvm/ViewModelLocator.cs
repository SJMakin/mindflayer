using MindFlayer.ui.model;

namespace MindFlayer
{
    internal class ViewModelLocator
    {
        public ChatViewModel ChatViewModel { get; } = new();
        public ImageViewerModel ImageViewerModel { get; } = new();
        public TranscribeModel TranscribeModel { get; } = new();
    }
}
