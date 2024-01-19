using MindFlayer.ui.model;

namespace MindFlayer
{
    internal class ViewModelLocator
    {
        public ChatViewModel ChatViewModel { get; } = new ChatViewModel();
        public ImageViewerModel ImageViewerModel { get; } = new ImageViewerModel();
    }
}
