using OpenAI;
using OpenAI.Images;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MindFlayer.ui.model
{
    internal class ImageViewerModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    
        private static readonly OpenAIClient Client = new(OpenAIAuthentication.LoadFromEnv());

        private string LastPrompt { get; set; }

        private BitmapSource _currentImage;

        public BitmapSource CurrentImage
        {
            get => _currentImage;
            set
            {
                _currentImage = value;
                OnPropertyChanged(nameof(CurrentImage));
            }
        }
        private ICommand _loadCommand;
        public ICommand LoadCommand => _loadCommand ??= new RelayCommand(() => true, Load);

        private void Load()
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*";
            if (!(ofd.ShowDialog() == DialogResult.OK)) return;
            CurrentImage = new BitmapImage(new Uri(ofd.FileName));
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new RelayCommand(() => true, Save);

        private void Save()
        {
            using var img = BitmapFromSource(CurrentImage);
            using var sfd = new SaveFileDialog();
            sfd.Filter = "Image Files(*.PNG)|*.png|All files (*.*)|*.*";
            if (!(sfd.ShowDialog() == DialogResult.OK)) return;
            img.Save(sfd.FileName, ImageFormat.Png);
            Clipboard.SetImage(img);
        }

        private ICommand _promptCommand;
        public ICommand PromptCommand => _promptCommand ??= new RelayCommand(() => true, Prompt);

        private void Prompt()
        {
            var pd = new PromptDialog(LastPrompt);
            if (!pd.ShowDialog().GetValueOrDefault()) return;
            LastPrompt = pd.PromptResult;
            var result = Client.ImagesEndPoint.GenerateImageAsync(new ImageGenerationRequest(pd.PromptResult, model: OpenAI.Models.Model.DallE_3, responseFormat: ResponseFormat.Url)).Result;
            var images = result.Select(DownloadImage).ToList();
            CurrentImage = images.FirstOrDefault();
        }

        private ICommand _editCommand;
        public ICommand EditCommand => _editCommand ??= new RelayCommand(() => true, Edit);

        private void Edit()
        {
            using var img = BitmapFromSource(CurrentImage);
            const string imagePath = @"c:\temp\edit.png";
            img.Save(imagePath, ImageFormat.Png);
            using var req = new ImageVariationRequest(imagePath);
            var result = Client.ImagesEndPoint.CreateImageVariationAsync(req).Result;
            var images = result.Select(DownloadImage).ToList();
            CurrentImage = images.FirstOrDefault();
        }

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= new RelayCommand(() => true, Copy);

        private void Copy()
        {
            using var img = BitmapFromSource(CurrentImage);
            Clipboard.SetImage(img);
        }

        public static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            //convert image format
            var src = new FormatConvertedBitmap();
            src.BeginInit();
            src.Source = bitmapsource;
            src.DestinationFormat = System.Windows.Media.PixelFormats.Bgra32;
            src.EndInit();

            //copy to bitmap
            Bitmap bitmap = new Bitmap(src.PixelWidth, src.PixelHeight, PixelFormat.Format32bppArgb);
            var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            src.CopyPixels(System.Windows.Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bitmap.UnlockBits(data);

            return bitmap;
        }

        public BitmapSource DownloadImage(string url)
        {
            return new BitmapImage(new Uri(url));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
