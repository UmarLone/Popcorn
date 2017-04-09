using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Popcorn.ImageLoader.ImageLoaders
{
    class ExternalLoader : ILoader
    {
        #region ILoader Members

        public async Task<Stream> Load(string source)
        {
            using (var client = new HttpClient())
            {
                var data = await client.GetByteArrayAsync(source);

                if (data == null || data.Count() == 0) return null;

                return new MemoryStream(data);
            }
        }

        #endregion
    }
}