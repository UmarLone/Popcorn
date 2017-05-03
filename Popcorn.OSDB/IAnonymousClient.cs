using System;
using System.Collections.Generic;

namespace Popcorn.OSDB
{
    public interface IAnonymousClient : IDisposable
    {
        IList<Subtitle> SearchSubtitlesFromImdb(string languages, string imdbId);
        string DownloadSubtitleToPath(string path, Subtitle subtitle);
        IEnumerable<Language> GetSubLanguages();
    }
}