using System.IO;
using System.Threading.Tasks;

namespace Popcorn.ImageLoader.ImageLoaders
{
    internal interface ILoader
    {
        Task<Stream> Load(string source);
    }
}
