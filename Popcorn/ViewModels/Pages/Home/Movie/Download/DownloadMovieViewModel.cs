using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using lt;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Movie;
using Popcorn.Services.Language;
using Popcorn.Services.Subtitles;
using Popcorn.ViewModels.Windows.Settings;

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
        /// Manage th application settings
        /// </summary>
        private readonly ApplicationSettingsViewModel _applicationSettingsViewModel;

        /// <summary>
        /// Token to cancel the download
        /// </summary>
        private CancellationTokenSource _cancellationDownloadingMovie;

        /// <summary>
        /// Specify if a movie is downloading
        /// </summary>
        private bool _isDownloadingMovie;

        /// <summary>
        /// Specify if a movie is buffered
        /// </summary>
        private bool _isMovieBuffered;

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
        /// Movie file path
        /// </summary>
        private string _movieFilePath;

        /// <summary>
        /// The download progress
        /// </summary>
        private Progress<double> _reportDownloadProgress;

        /// <summary>
        /// Initializes a new instance of the DownloadMovieViewModel class.
        /// </summary>
        /// <param name="subtitlesService">Instance of SubtitlesService</param>
        /// <param name="languageService">Language service</param>
        public DownloadMovieViewModel(ISubtitlesService subtitlesService, ILanguageService languageService)
        {
            _subtitlesService = subtitlesService;
            _applicationSettingsViewModel = new ApplicationSettingsViewModel(languageService);
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
                "Stop downloading a movie");

            IsDownloadingMovie = false;
            _isMovieBuffered = false;
            _cancellationDownloadingMovie.Cancel(true);
            _cancellationDownloadingMovie = new CancellationTokenSource();

            if (!string.IsNullOrEmpty(_movieFilePath))
            {
                try
                {
                    File.Delete(_movieFilePath);
                    _movieFilePath = string.Empty;
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
                Movie = message.Movie;
                MovieDownloadRate = 0d;
                MovieDownloadProgress = 0d;
                NbPeers = 0;
                NbSeeders = 0;
                _reportDownloadProgress = new Progress<double>(ReportMovieDownloadProgress);
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
                            var path = Path.Combine(Constants.Constants.Subtitles + message.Movie.ImdbCode);
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
                            await
                                DownloadMovieAsync(message.Movie,
                                    _reportDownloadProgress, reportDownloadRate, reportNbSeeders, reportNbPeers,
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
            if (value < Constants.Constants.MinimumMovieBuffering)
                return;

            if (!_isMovieBuffered)
                _isMovieBuffered = true;
        }

        /// <summary>
        /// Download a movie asynchronously
        /// </summary>
        /// <param name="movie">The movie to download</param>
        /// <param name="downloadProgress">Report download progress</param>
        /// <param name="downloadRate">Report download rate</param>
        /// <param name="nbSeeds">Report number of seeders</param>
        /// <param name="nbPeers">Report number of peers</param>
        /// <param name="ct">Cancellation token</param>
        private async Task DownloadMovieAsync(MovieJson movie, IProgress<double> downloadProgress,
            IProgress<double> downloadRate, IProgress<int> nbSeeds, IProgress<int> nbPeers,
            CancellationTokenSource ct)
        {
            _movieFilePath = string.Empty;
            MovieDownloadProgress = 0d;
            MovieDownloadRate = 0d;
            NbSeeders = 0;
            NbPeers = 0;

            await Task.Run(async () =>
            {
                using (var session = new session())
                {
                    Logger.Info(
                        $"Start downloading movie : {movie.Title}");

                    IsDownloadingMovie = true;

                    downloadProgress?.Report(0d);
                    downloadRate?.Report(0d);
                    nbSeeds?.Report(0);
                    nbPeers?.Report(0);

                    session.listen_on(6881, 6889);
                    var torrentUrl = movie.WatchInFullHdQuality
                        ? movie.Torrents?.FirstOrDefault(torrent => torrent.Quality == "1080p")?.Url
                        : movie.Torrents?.FirstOrDefault(torrent => torrent.Quality == "720p")?.Url;

                    var result =
                        await
                            DownloadFileHelper.DownloadFileTaskAsync(torrentUrl,
                                Constants.Constants.MovieTorrentDownloads + movie.ImdbCode + ".torrent");
                    var torrentPath = string.Empty;
                    if (result.Item3 == null && !string.IsNullOrEmpty(result.Item2))
                        torrentPath = result.Item2;

                    using (var addParams = new add_torrent_params
                    {
                        save_path = Constants.Constants.MovieDownloads,
                        ti = new torrent_info(torrentPath)
                    })
                    using (var handle = session.add_torrent(addParams))
                    {
                        handle.set_upload_limit(_applicationSettingsViewModel.DownloadLimit * 1024);
                        handle.set_download_limit(_applicationSettingsViewModel.UploadLimit * 1024);

                        // We have to download sequentially, so that we're able to play the movie without waiting
                        handle.set_sequential_download(true);
                        var alreadyBuffered = false;
                        while (IsDownloadingMovie)
                        {
                            using (var status = handle.status())
                            {
                                var progress = status.progress * 100d;

                                nbSeeds?.Report(status.num_seeds);
                                nbPeers?.Report(status.num_peers);
                                downloadProgress?.Report(progress);
                                downloadRate?.Report(Math.Round(status.download_rate / 1024d, 0));

                                handle.flush_cache();
                                if (handle.need_save_resume_data())
                                    handle.save_resume_data(1);

                                if (progress >= Constants.Constants.MinimumMovieBuffering && !alreadyBuffered)
                                {
                                    // Get movie file
                                    foreach (
                                        var filePath in
                                        Directory
                                            .GetFiles(status.save_path, "*.*",
                                                SearchOption.AllDirectories)
                                            .Where(s => s.Contains(handle.torrent_file().name()) &&
                                                        (s.EndsWith(".mp4") || s.EndsWith(".mkv") ||
                                                         s.EndsWith(".mov") || s.EndsWith(".avi")))
                                    )
                                    {
                                        _movieFilePath = filePath;
                                        alreadyBuffered = true;
                                        movie.FilePath = filePath;
                                        Messenger.Default.Send(new PlayMovieMessage(movie, _reportDownloadProgress));
                                    }
                                }

                                try
                                {
                                    await Task.Delay(1000, ct.Token);
                                }
                                catch (TaskCanceledException)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }, ct.Token);
        }
    }
}