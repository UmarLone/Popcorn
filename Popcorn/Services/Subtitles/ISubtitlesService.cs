using System.Collections.Generic;
using Popcorn.OSDB;

namespace Popcorn.Services.Subtitles
{
    /// <summary>
    /// The subtitles service
    /// </summary>
    public interface ISubtitlesService
    {
        /// <summary>
        /// Get subtitles languages
        /// </summary>
        /// <returns>Languages</returns>
        IEnumerable<Language> GetSubLanguages();

        /// <summary>
        /// Search subtitles by imdb code and languages
        /// </summary>
        /// <param name="languages">Languages</param>
        /// <param name="imdbId">Imdb code</param>
        /// <returns></returns>
        IList<Subtitle> SearchSubtitlesFromImdb(string languages, string imdbId);

        /// <summary>
        /// Download a subtitle to a path
        /// </summary>
        /// <param name="path">Path to download</param>
        /// <param name="subtitle">Subtitle to download</param>
        /// <returns>Downloaded subtitle path</returns>
        string DownloadSubtitleToPath(string path, Subtitle subtitle);
    }
}
