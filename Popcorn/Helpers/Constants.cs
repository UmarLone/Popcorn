using System.IO;

namespace Popcorn.Helpers
{
    /// <summary>
    /// Constants of the project
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// App version
        /// </summary>
        public const string AppVersion = "1.9.21";
        
        /// <summary>
        /// Endpoint to API
        /// </summary>
        public const string PopcornApi = "https://popcornapi.azurewebsites.net/api";

        /// <summary>
        /// Client ID for TMDb
        /// </summary>
        public const string TmDbClientId = "a21fe922d3bac6654e93450e9a18af1c";

        /// <summary>
        /// In percentage, the minimum of buffering before we can actually start playing the media
        /// </summary>
        public const double MinimumBuffering = 2.0;

        /// <summary>
        /// The maximum number of movies per page to load from the API
        /// </summary>
        public const int MaxMoviesPerPage = 20;

        /// <summary>
        /// The maximum number of shows per page to load from the API
        /// </summary>
        public const int MaxShowsPerPage = 20;

        /// <summary>
        /// Url of the server updates
        /// </summary>
        public const string GithubRepository = "https://github.com/bbougot/Popcorn";

        /// <summary>
        /// Directory of downloaded movies
        /// </summary>
        public static string MovieDownloads { get; } = Path.GetTempPath() + "Popcorn\\Downloads\\Movies\\";

        /// <summary>
        /// Directory of downloaded shows
        /// </summary>
        public static string ShowDownloads { get; } = Path.GetTempPath() + "Popcorn\\Downloads\\Shows\\";

        /// <summary>
        /// Directory of downloaded movie torrents
        /// </summary>
        public static string MovieTorrentDownloads { get; } = Path.GetTempPath() + "Popcorn\\Torrents\\Movies\\";

        /// <summary>
        /// Subtitles directory
        /// </summary>
        public static string Subtitles { get; } = Path.GetTempPath() + "Popcorn\\Subtitles\\";

        /// <summary>
        /// Logging directory
        /// </summary>
        public static string Logging { get; } = Path.GetTempPath() + "Popcorn\\Logs\\";
    }
}