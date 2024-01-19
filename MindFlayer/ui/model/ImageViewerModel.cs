using OpenAI;
using OpenAI.Images;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MindFlayer.ui.model
{
    internal class ImageViewerModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    
        private static readonly OpenAIClient Client = new(OpenAIAuthentication.LoadFromEnv());

        private Image _currentImage;

        public Image CurrentImage
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
            CurrentImage = Image.FromFile(@"c:\temp\pineapple.jpg");
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new RelayCommand(() => true, Save);

        private void Save()
        {
            throw new NotImplementedException();
        }

        private ICommand _promptCommand;
        public ICommand PromptCommand => _promptCommand ??= new RelayCommand(() => true, Prompt);

        private void Prompt()
        {
            var result = Client.ImagesEndPoint.GenerateImageAsync(new ImageGenerationRequest("pineapples", model: OpenAI.Models.Model.DallE_3, responseFormat: ResponseFormat.Url)).Result;
            var images = result.Select(DownloadImage).ToList();
            CurrentImage = images.FirstOrDefault();
        }

        public Image DownloadImage(string url)
        {
            Image image;
            using (WebClient webClient = new WebClient())
            {
                using (Stream stream = webClient.OpenRead(url))
                {
                    image = Image.FromStream(stream);
                }
            }
            return image;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
