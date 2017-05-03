using System.Collections.Generic;
using Popcorn.OSDB;

namespace Popcorn.Services.Subtitles
{
    /// <summary>
    /// The subtitles service
    /// </summary>
    public class SubtitlesService : ISubtitlesService
    {
        /// <summary>
        /// Get subtitles languages
        /// </summary>
        /// <returns>Languages</returns>
        public IEnumerable<Language> GetSubLanguages()
        {
            using (var osdb = new Osdb().Login("OSTestUserAgentTemp"))
            {
                return osdb.GetSubLanguages();
            }
        }

        /// <summary>
        /// Search subtitles by imdb code and languages
        /// </summary>
        /// <param name="languages">Languages</param>
        /// <param name="imdbId">Imdb code</param>
        /// <returns></returns>
        public IList<Subtitle> SearchSubtitlesFromImdb(string languages, string imdbId)
        {
            using (var osdb = new Osdb().Login("OSTestUserAgentTemp"))
            {
                return osdb.SearchSubtitlesFromImdb(languages, imdbId);
            }
        }

        /// <summary>
        /// Download a subtitle to a path
        /// </summary>
        /// <param name="path">Path to download</param>
        /// <param name="subtitle">Subtitle to download</param>
        /// <returns>Downloaded subtitle path</returns>
        public string DownloadSubtitleToPath(string path, Subtitle subtitle)
        {
            using (var osdb = new Osdb().Login("OSTestUserAgentTemp"))
            {
                return osdb.DownloadSubtitleToPath(path, subtitle);
            }
        }
    }
}