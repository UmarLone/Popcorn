using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Movie;
using Popcorn.Services.Subtitles;
using Popcorn.Utils;
using Popcorn.ViewModels.Windows.Settings;
using Popcorn.Services.Download;
using GalaSoft.MvvmLight.Ioc;

namespace Popcorn.ViewModels.Pages.Home.Movie.Download
{
    /// <summary>
    /// Manage the download of a movie
    /// </summary>
    public sealed class DownloadMovieViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Used to interact with subtitles
        /// </summary>
        private readonly ISubtitlesService _subtitlesService;

        /// <summary>
        /// The download service
        /// </summary>
        private readonly IDownloadService<MovieJson> _downloadService;

        /// <summary>
        /// Token to cancel the download
        /// </summary>
        private CancellationTokenSource _cancellationDownloadingMovie;

        /// <summary>
        /// Specify if a movie is downloading
        /// </summary>
        private bool _isDownloadingMovie;

        /// <summary>
        /// The movie to download
        /// </summary>
        private MovieJson _movie;

        /// <summary>
        /// The movie download progress
        /// </summary>
        private double _movieDownloadProgress;

        /// <summary>
        /// The movie download rate
        /// </summary>
        private double _movieDownloadRate;

        /// <summary>
        /// Number of seeders
        /// </summary>
        private int _nbSeeders;

        /// <summary>
        /// Number of peers
        /// </summary>
        private int _nbPeers;

        /// <summary>
        /// Initializes a new instance of the DownloadMovieViewModel class.
        /// </summary>
        /// <param name="subtitlesService">Instance of SubtitlesService</param>
        /// <param name="downloadService">Download service</param>
        public DownloadMovieViewModel(ISubtitlesService subtitlesService, IDownloadService<MovieJson> downloadService)
        {
            _subtitlesService = subtitlesService;
            _downloadService = downloadService;
            _cancellationDownloadingMovie = new CancellationTokenSource();
            RegisterMessages();
            RegisterCommands();
        }

        /// <summary>
        /// Specify if a movie is downloading
        /// </summary>
        public bool IsDownloadingMovie
        {
            get => _isDownloadingMovie;
            set { Set(() => IsDownloadingMovie, ref _isDownloadingMovie, value); }
        }

        /// <summary>
        /// Specify the movie download progress
        /// </summary>
        public double MovieDownloadProgress
        {
            get => _movieDownloadProgress;
            set { Set(() => MovieDownloadProgress, ref _movieDownloadProgress, value); }
        }

        /// <summary>
        /// Specify the movie download rate
        /// </summary>
        public double MovieDownloadRate
        {
            get => _movieDownloadRate;
            set { Set(() => MovieDownloadRate, ref _movieDownloadRate, value); }
        }

        /// <summary>
        /// Number of peers
        /// </summary>
        public int NbPeers
        {
            get => _nbPeers;
            set { Set(() => NbPeers, ref _nbPeers, value); }
        }

        /// <summary>
        /// Number of seeders
        /// </summary>
        public int NbSeeders
        {
            get => _nbSeeders;
            set { Set(() => NbSeeders, ref _nbSeeders, value); }
        }

        /// <summary>
        /// The movie to download
        /// </summary>
        public MovieJson Movie
        {
            get => _movie;
            set { Set(() => Movie, ref _movie, value); }
        }

        /// <summary>
        /// The command used to stop the download of a movie
        /// </summary>
        public RelayCommand StopDownloadingMovieCommand { get; private set; }

        /// <summary>
        /// Stop downloading a movie
        /// </summary>
        public void StopDownloadingMovie()
        {
            Logger.Info(
                $"Stop downloading the movie {Movie.Title}.");

            IsDownloadingMovie = false;
            _cancellationDownloadingMovie.Cancel(true);
            _cancellationDownloadingMovie = new CancellationTokenSource();

            if (!string.IsNullOrEmpty(Movie?.FilePath))
            {
                try
                {
                    File.Delete(Movie.FilePath);
                    Movie.FilePath = string.Empty;
                }
                catch (Exception)
                {
                    // File could not be deleted... We don't care
                }
            }

        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public override void Cleanup()
        {
            StopDownloadingMovie();
            base.Cleanup();
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages() => Messenger.Default.Register<DownloadMovieMessage>(
            this,
            message =>
            {
                IsDownloadingMovie = true;
                Movie = message.Movie;
                MovieDownloadRate = 0d;
                MovieDownloadProgress = 0d;
                NbPeers = 0;
                NbSeeders = 0;
                var reportDownloadProgress = new Progress<double>(ReportMovieDownloadProgress);
                var reportDownloadRate = new Progress<double>(ReportMovieDownloadRate);
                var reportNbPeers = new Progress<int>(ReportNbPeers);
                var reportNbSeeders = new Progress<int>(ReportNbSeeders);

                Task.Run(async () =>
                {
                    try
                    {
                        if (message.Movie.SelectedSubtitle != null &&
                            message.Movie.SelectedSubtitle.Sub.LanguageName !=
                            LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"))
                        {
                            var path = Path.Combine(Constants.Subtitles + message.Movie.ImdbCode);
                            Directory.CreateDirectory(path);
                            var subtitlePath =
                                _subtitlesService.DownloadSubtitleToPath(path,
                                    message.Movie.SelectedSubtitle.Sub);

                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            {
                                message.Movie.SelectedSubtitle.FilePath = subtitlePath;
                            });
                        }
                    }
                    finally
                    {
                        try
                        {
                            var torrentUrl = Movie.WatchInFullHdQuality
                                ? Movie.Torrents?.FirstOrDefault(torrent => torrent.Quality == "1080p")?.Url
                                : Movie.Torrents?.FirstOrDefault(torrent => torrent.Quality == "720p")?.Url;

                            var result =
                                await
                                    DownloadFileHelper.DownloadFileTaskAsync(torrentUrl,
                                        Constants.MovieTorrentDownloads + Movie.ImdbCode + ".torrent");
                            var torrentPath = string.Empty;
                            if (result.Item3 == null && !string.IsNullOrEmpty(result.Item2))
                                torrentPath = result.Item2;

                            var settings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                            await _downloadService.Download(Movie, TorrentType.File, MediaType.Movie, torrentPath,
                                settings.UploadLimit, settings.DownloadLimit, reportDownloadProgress,
                                reportDownloadRate, reportNbSeeders, reportNbPeers, () => { }, () => { },
                                _cancellationDownloadingMovie);
                        }
                        catch (Exception ex)
                        {
                            // An error occured.
                            Messenger.Default.Send(new ManageExceptionMessage(ex));
                            Messenger.Default.Send(new StopPlayingMovieMessage());
                        }
                    }
                });
            });

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands() => StopDownloadingMovieCommand = new RelayCommand(() =>
        {
            Messenger.Default.Send(new StopPlayingMovieMessage());
        });

        /// <summary>
        /// Report the number of seeders
        /// </summary>
        /// <param name="value">Number of seeders</param>
        private void ReportNbSeeders(int value) => NbSeeders = value;

        /// <summary>
        /// Report the number of peers
        /// </summary>
        /// <param name="value">Nubmer of peers</param>
        private void ReportNbPeers(int value) => NbPeers = value;

        /// <summary>
        /// Report the download progress
        /// </summary>
        /// <param name="value">Download rate</param>
        private void ReportMovieDownloadRate(double value) => MovieDownloadRate = value;

        /// <summary>
        /// Report the download progress
        /// </summary>
        /// <param name="value">The download progress to report</param>
        private void ReportMovieDownloadProgress(double value)
        {
            MovieDownloadProgress = value;
        }
    }
}