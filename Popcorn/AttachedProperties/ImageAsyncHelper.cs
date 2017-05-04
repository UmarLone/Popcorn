using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Threading;
using Popcorn.Utils;

namespace Popcorn.AttachedProperties
{
    /// <summary>
    /// Image async
    /// </summary>
    public class ImageAsyncHelper : DependencyObject
    {
        /// <summary>
        /// Get source uri
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetImagePath(DependencyObject obj)
        {
            return (string) obj.GetValue(ImagePathProperty);
        }

        /// <summary>
        /// Set source uri
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetImagePath(DependencyObject obj, Uri value)
        {
            obj.SetValue(ImagePathProperty, value);
        }

        /// <summary>
        /// Image path property
        /// </summary>
        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.RegisterAttached("ImagePath",
                typeof(string),
                typeof(ImageAsyncHelper),
                new PropertyMetadata
                {
                    PropertyChangedCallback = (obj, e) =>
                    {
                        Task.Run(async () =>
                        {
                            var image = (Image)obj;
                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            {
                                image.Source = null;
                            });

                            var path = e.NewValue as string;
                            if (string.IsNullOrEmpty(path)) return;
                            var localFile = string.Empty;
                            var fileName = path.Substring(path.LastIndexOf("/images/", StringComparison.InvariantCulture) +
                                                          1);
                            fileName = fileName.Replace('/', '_');
                            var files = FastDirectoryEnumerator.EnumerateFiles(Constants.Assets);
                            var file = files.FirstOrDefault(a => a.Name.Contains(fileName));
                            if (file != null)
                            {
                                localFile = file.Path;
                            }
                            else
                            {
                                using (var client = new HttpClient())
                                {
                                    var bytes = await client.GetByteArrayAsync(path);
                                    {
                                        if (bytes == null || bytes.Length == 0) return;
                                        File.WriteAllBytes(Constants.Assets + fileName, bytes);
                                    }
                                }

                                localFile = Constants.Assets + fileName;
                            }

                            var data = File.ReadAllBytes(localFile);
                            {
                                if (data.Length == 0) return;
                                var bitmapImage = new BitmapImage();
                                using (var stream = new MemoryStream(data))
                                {
                                    bitmapImage.BeginInit();
                                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmapImage.StreamSource = stream;
                                    bitmapImage.EndInit();
                                    bitmapImage.Freeze();
                                }

                                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                {
                                    image.Source = bitmapImage;
                                });
                            }
                        });
                    }
                }
            );
    }
}