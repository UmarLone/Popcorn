using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Popcorn.IO;

namespace Popcorn.ImageLoader.ImageLoaders
{
    class ExternalLoader : ILoader
    {
        #region ILoader Members

        public async Task<Stream> Load(string source)
        {
            var fileName = source.Substring(source.LastIndexOf("/images/", StringComparison.InvariantCulture) + 1);
            fileName = fileName.Replace('/', '_');
            if (!Directory.Exists(Constants.Constants.Assets))
            {
                Directory.CreateDirectory(Constants.Constants.Assets);
            }

            var files = FastDirectoryEnumerator.EnumerateFiles(Constants.Constants.Assets);
            var file = files.FirstOrDefault(a => a.Name.Contains(fileName));
            if (file != null)
            {
                return File.OpenRead(file.Path);
            }

            using (var client = new HttpClient())
            {
                var data = await client.GetByteArrayAsync(source);
                {
                    if (data == null || data.Length == 0) return null;
                    File.WriteAllBytes(Constants.Constants.Assets + fileName, data);
                    return new MemoryStream(data);
                }
            }
        }

        #endregion
    }
}