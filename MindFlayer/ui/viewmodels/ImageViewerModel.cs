using Microsoft.Win32;
using OpenAI;
using OpenAI.Images;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MindFlayer.ui.model;

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
        if (!(ofd.ShowDialog() == true)) return;
        CurrentImage = new BitmapImage(new Uri(ofd.FileName));
    }

    private ICommand _saveCommand;
    public ICommand SaveCommand => _saveCommand ??= new RelayCommand(() => true, Save);

    private void Save()
    {
        var sfd = new SaveFileDialog();
        sfd.Filter = "Image Files(*.PNG)|*.png|All files (*.*)|*.*";
        if (!(sfd.ShowDialog() == true)) return;
        using (var fileStream = new FileStream(sfd.FileName, FileMode.Create))
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(CurrentImage));
            encoder.Save(fileStream);
        }
        Clipboard.SetImage(CurrentImage);
    }

    private ICommand _promptCommand;
    public ICommand PromptCommand => _promptCommand ??= new RelayCommand(() => true, Prompt);

    private void Prompt()
    {
        var pd = new PromptDialog(LastPrompt);
        if (!pd.ShowDialog().GetValueOrDefault()) return;
        LastPrompt = pd.PromptResult;
        var result = Client.ImagesEndPoint.GenerateImageAsync(new ImageGenerationRequest(pd.PromptResult, model: OpenAI.Models.Model.DallE_3, responseFormat: ImageResponseFormat.Url)).Result;
        var images = result.Select(DownloadImage).ToList();
        CurrentImage = images.FirstOrDefault();
    }

    private ICommand _editCommand;
    public ICommand EditCommand => _editCommand ??= new RelayCommand(() => true, Edit);

    private void Edit()
    {
        const string imagePath = @"c:\temp\edit.png";
        using (var fileStream = new FileStream(imagePath, FileMode.Create))
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(CurrentImage));
            encoder.Save(fileStream);
        }

        using var req = new ImageVariationRequest(imagePath);
        var result = Client.ImagesEndPoint.CreateImageVariationAsync(req).Result;
        var images = result.Select(DownloadImage).ToList();
        CurrentImage = images.FirstOrDefault();
    }

    private ICommand _copyCommand;
    public ICommand CopyCommand => _copyCommand ??= new RelayCommand(() => true, Copy);

    private void Copy()
    {
        Clipboard.SetImage(CurrentImage);
    }

    public BitmapSource DownloadImage(ImageResult ir)
    {
        return new BitmapImage(new Uri(ir.Url));
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
