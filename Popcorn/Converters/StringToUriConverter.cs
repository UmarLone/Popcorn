using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Data;
using Popcorn.IO;

namespace Popcorn.Converters
{
    /// <summary>
    /// Used to check if the path to the image file is empty or not
    /// </summary>
    public class StringToUriConverter : IValueConverter
    {
        /// <summary>
        /// Convert a path image to a bitmap-cached image
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Cached image</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value?.ToString())) return null;
            var path = value.ToString();
            var fileName = path.Substring(path.LastIndexOf("/images/", StringComparison.InvariantCulture) + 1);
            fileName = fileName.Replace('/', '_');
            var files = FastDirectoryEnumerator.EnumerateFiles(Utils.Constants.Assets);
            var file = files.FirstOrDefault(a => a.Name.Contains(fileName));
            if (file != null)
            {
                return new Uri(file.Path, UriKind.Absolute);
            }

            Task.Run(async () =>
            {
                using (var client = new HttpClient())
                {
                    var data = await client.GetByteArrayAsync(path);
                    {
                        if (data == null || data.Length == 0) return;
                        File.WriteAllBytes(Utils.Constants.Assets + fileName, data);
                    }
                }
            });

            return new Uri(path, UriKind.Absolute);
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}