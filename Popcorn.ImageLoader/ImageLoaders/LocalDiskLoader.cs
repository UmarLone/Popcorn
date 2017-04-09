using System.IO;
using System.Threading.Tasks;

namespace Popcorn.ImageLoader.ImageLoaders
{
    internal class LocalDiskLoader : ILoader
    {
        public async Task<Stream> Load(string source)
        {
            return await Task.FromResult<Stream>(File.OpenRead(source));
        }
    }
}