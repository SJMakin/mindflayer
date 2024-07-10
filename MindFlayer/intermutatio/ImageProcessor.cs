using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Media.Imaging;

namespace MindFlayer.intermutatio;

public static class ImageProcessor
{
    public static (string, int) ProcessImage(string path, int maxSize)
    {
        using var image = Image.FromFile(path);
        int width = image.Width;
        int height = image.Height;
        string mimeType = GetImageMimeType(image);

        if (mimeType == "image/png" && width <= maxSize && height <= maxSize)
        {
            byte[] imageBytes = File.ReadAllBytes(path);
            string encodedImage = Convert.ToBase64String(imageBytes);
            return (encodedImage, Math.Max(width, height));
        }
        else
        {
            using var resizedImage = ResizeImage(image, maxSize);
            byte[] pngImage = ConvertToPng(resizedImage);
            string encodedImage = Convert.ToBase64String(pngImage);
            return (encodedImage, Math.Max(width, height));
        }
    }

    private static string GetImageMimeType(Image image)
    {
        if (image.RawFormat.Equals(ImageFormat.Png)) return "image/png";
        if (image.RawFormat.Equals(ImageFormat.Jpeg)) return "image/jpeg";
        if (image.RawFormat.Equals(ImageFormat.Gif)) return "image/gif";
        return "application/octet-stream";
    }

    private static Image ResizeImage(Image image, int maxDimension)
    {
        int width = image.Width;
        int height = image.Height;

        if (width > maxDimension || height > maxDimension)
        {
            if (width > height)
            {
                int newWidth = maxDimension;
                int newHeight = (int)(height * ((double)maxDimension / width));
                var resizedImage = new Bitmap(newWidth, newHeight);
                using (var graphics = Graphics.FromImage(resizedImage))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                }
                return resizedImage;
            }
            else
            {
                int newHeight = maxDimension;
                int newWidth = (int)(width * ((double)maxDimension / height));
                var resizedImage = new Bitmap(newWidth, newHeight);
                using (var graphics = Graphics.FromImage(resizedImage))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                }
                return resizedImage;
            }
        }

        return image;
    }

    private static byte[] ConvertToPng(Image image)
    {
        using var ms = new MemoryStream();
        image.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public static string CreateData(string image) => $"data:image/png;base64,{image}";

    public static BitmapImage GetWpfImageSource(string path, int maxSize)
    {
        var (encodedImage, _) = ProcessImage(path, maxSize);
        byte[] imageBytes = Convert.FromBase64String(encodedImage);

        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = new MemoryStream(imageBytes);
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze(); // Makes it thread-safe

        return bitmapImage;
    }
}
