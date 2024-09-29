using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace MindFlayer
{
    public class Attachment
    {
        public string FileName { get; set; }
        public string Base64Content { get; set; }
        public FileType AttachmentType { get; set; }

        public enum FileType
        {
            Image,
            Video,
            Audio,
            Document,
            Other
        }

        public ImageSource ThumbnailSource
        {
            get
            {
                if (AttachmentType == FileType.Image && !string.IsNullOrEmpty(Base64Content))
                {
                    byte[] imageBytes = Convert.FromBase64String(Base64Content);
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        return bitmap;
                    }
                }
                if (AttachmentType == FileType.Image && !string.IsNullOrEmpty(FileName))
                {
                    return new BitmapImage(new Uri(FileName));
                }
                return null;
            }
        }

        public string FileExtension
        {
            get
            {
                return Path.GetExtension(FileName)?.ToLower();
            }
        }

        public long FileSizeInBytes
        {
            get
            {
                if (!string.IsNullOrEmpty(Base64Content))
                {
                    return Convert.FromBase64String(Base64Content).Length;
                }
                if (!string.IsNullOrEmpty(FileName) && File.Exists(FileName))
                {
                    return new FileInfo(FileName).Length;
                }
                return 0;
            }
        }

        public string Description { get; set; }

        public Attachment(string fileName = "", string base64Content = "", FileType fileType = FileType.Image)
        {
            FileName = fileName;
            Base64Content = base64Content;
            AttachmentType = fileType;
        }
    }
}