using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Popcorn.OSDB
{
    public interface IAnonymousClient : IDisposable
    {
        Task<IList<Subtitle>> SearchSubtitlesFromImdb(string languages, string imdbId);
        Task<string> DownloadSubtitleToPath(string path, Subtitle subtitle);
        Task<IEnumerable<Language>> GetSubLanguages();
    }
}