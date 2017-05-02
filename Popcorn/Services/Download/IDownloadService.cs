using Popcorn.Models.Media;
using Popcorn.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Popcorn.Services.Download
{
    public interface IDownloadService<T> where T : IMediaFile
    {
        Task Download(T media, TorrentType torrentType, MediaType mediaType, string torrentPath, int uploadLimit, int downloadLimit, IProgress<double> downloadProgress,
            IProgress<double> downloadRate, IProgress<int> nbSeeds, IProgress<int> nbPeers, Action buffered, Action cancelled,
            CancellationTokenSource cts);
    }
}