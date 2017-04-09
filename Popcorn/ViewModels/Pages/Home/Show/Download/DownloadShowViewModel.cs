using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using lt;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Episode;
using Popcorn.Services.Language;
using Popcorn.Services.Subtitles;
using Popcorn.ViewModels.Windows.Settings;

namespace Popcorn.ViewModels.Pages.Home.Show.Download
{
    public class DownloadShowViewModel : ViewModelBase
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
        /// Manage the application settings
        /// </summary>
        private readonly ApplicationSettingsViewModel _applicationSettingsViewModel;

        /// <summary>
        /// Token to cancel the download
        /// </summary>
        private CancellationTokenSource _cancellationDownloadingEpisode;

        /// <summary>
        /// Specify if an episode is downloading
        /// </summary>
        private bool _isDownloadingEpisode;

        /// <summary>
        /// Specify if an episode is buffered
        /// </summary>
        private bool _isEpisodeBuffered;

        /// <summary>
        /// The episode to download
        /// </summary>
        private EpisodeShowJson _episode;

        /// <summary>
        /// The episode download progress
        /// </summary>
        private double _episodeDownloadProgress;

        /// <summary>
        /// The episode download rate
        /// </summary>
        private double _episodeDownloadRate;

        /// <summary>
        /// Number of seeders
        /// </summary>
        private int _nbSeeders;

        /// <summary>
        /// Number of peers
        /// </summary>
        private int _nbPeers;

        /// <summary>
        /// Episode file path
        /// </summary>
        private string _episodeFilePath;

        /// <summary>
        /// The download progress
        /// </summary>
        private Progress<double> _reportDownloadProgress;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="languageService">The language service</param>
        public DownloadShowViewModel(ILanguageService languageService)
        {
            RegisterCommands();
            RegisterMessages();

            _applicationSettingsViewModel = new ApplicationSettingsViewModel(languageService);
            _cancellationDownloadingEpisode = new CancellationTokenSource();
        }

        /// <summary>
        /// Specify if an episode is downloading
        /// </summary>
        public bool IsDownloadingEpisode
        {
            get => _isDownloadingEpisode;
            set { Set(() => IsDownloadingEpisode, ref _isDownloadingEpisode, value); }
        }

        /// <summary>
        /// Specify the episode download progress
        /// </summary>
        public double EpisodeDownloadProgress
        {
            get => _episodeDownloadProgress;
            set { Set(() => EpisodeDownloadProgress, ref _episodeDownloadProgress, value); }
        }

        /// <summary>
        /// Specify the episode download rate
        /// </summary>
        public double EpisodeDownloadRate
        {
            get => _episodeDownloadRate;
            set { Set(() => EpisodeDownloadRate, ref _episodeDownloadRate, value); }
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
        /// The episode to download
        /// </summary>
        public EpisodeShowJson Episode
        {
            get => _episode;
            set { Set(() => Episode, ref _episode, value); }
        }

        /// <summary>
        /// The command used to stop the download of an episode
        /// </summary>
        public RelayCommand StopDownloadingEpisodeCommand { get; private set; }

        /// <summary>
        /// Stop downloading an episode
        /// </summary>
        public void StopDownloadingEpisode()
        {
            Logger.Info(
                "Stop downloading an episode");

            IsDownloadingEpisode = false;
            _isEpisodeBuffered = false;
            _cancellationDownloadingEpisode.Cancel(true);
            _cancellationDownloadingEpisode = new CancellationTokenSource();

            if (!string.IsNullOrEmpty(_episodeFilePath))
            {
                try
                {
                    File.Delete(_episodeFilePath);
                    _episodeFilePath = string.Empty;
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
            StopDownloadingEpisode();
            base.Cleanup();
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages() => Messenger.Default.Register<DownloadShowEpisodeMessage>(
            this,
            message =>
            {
                Episode = message.Episode;
                EpisodeDownloadRate = 0d;
                EpisodeDownloadProgress = 0d;
                NbPeers = 0;
                NbSeeders = 0;
                _reportDownloadProgress = new Progress<double>(ReportEpisodeDownloadProgress);
                var reportDownloadRate = new Progress<double>(ReportEpisodeDownloadRate);
                var reportNbPeers = new Progress<int>(ReportNbPeers);
                var reportNbSeeders = new Progress<int>(ReportNbSeeders);

                Task.Run(async () =>
                {
                    try
                    {
                        if (message.Episode.SelectedSubtitle != null &&
                            message.Episode.SelectedSubtitle.Sub.LanguageName !=
                            LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"))
                        {
                            var path = Path.Combine(Constants.Constants.Subtitles + message.Episode.ImdbId);
                            Directory.CreateDirectory(path);
                            var subtitlePath =
                                _subtitlesService.DownloadSubtitleToPath(path,
                                    message.Episode.SelectedSubtitle.Sub);

                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            {
                                message.Episode.SelectedSubtitle.FilePath = subtitlePath;
                            });
                        }
                    }
                    finally
                    {
                        try
                        {
                            await
                                DownloadEpisodeAsync(message.Episode,
                                    _reportDownloadProgress, reportDownloadRate, reportNbSeeders, reportNbPeers,
                                    _cancellationDownloadingEpisode);
                        }
                        catch (Exception ex)
                        {
                            // An error occured.
                            Messenger.Default.Send(new ManageExceptionMessage(ex));
                            Messenger.Default.Send(new StopPlayingEpisodeMessage());
                        }
                    }
                });
            });

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands() => StopDownloadingEpisodeCommand = new RelayCommand(() =>
        {
            StopDownloadingEpisode();
            Messenger.Default.Send(new StopPlayingEpisodeMessage());
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
        private void ReportEpisodeDownloadRate(double value) => EpisodeDownloadRate = value;

        /// <summary>
        /// Report the download progress
        /// </summary>
        /// <param name="value">The download progress to report</param>
        private void ReportEpisodeDownloadProgress(double value)
        {
            EpisodeDownloadProgress = value;
            if (value < Constants.Constants.MinimumMovieBuffering)
                return;

            if (!_isEpisodeBuffered)
                _isEpisodeBuffered = true;
        }

        /// <summary>
        /// Download an episode asynchronously
        /// </summary>
        /// <param name="episode">The episode to download</param>
        /// <param name="downloadProgress">Report download progress</param>
        /// <param name="downloadRate">Report download rate</param>
        /// <param name="nbSeeds">Report number of seeders</param>
        /// <param name="nbPeers">Report number of peers</param>
        /// <param name="ct">Cancellation token</param>
        private async Task DownloadEpisodeAsync(EpisodeShowJson episode, IProgress<double> downloadProgress,
            IProgress<double> downloadRate, IProgress<int> nbSeeds, IProgress<int> nbPeers,
            CancellationTokenSource ct)
        {
            _episodeFilePath = string.Empty;
            EpisodeDownloadProgress = 0d;
            EpisodeDownloadRate = 0d;
            NbSeeders = 0;
            NbPeers = 0;

            await Task.Run(async () =>
            {
                using (var session = new session())
                {
                    Logger.Info(
                        $"Start downloading episode : {episode.Title}");

                    IsDownloadingEpisode = true;

                    downloadProgress?.Report(0d);
                    downloadRate?.Report(0d);
                    nbSeeds?.Report(0);
                    nbPeers?.Report(0);
                    session.listen_on(6881, 6889);
                    string magnetUri;
                    if (episode.WatchInFullHdQuality && (episode.Torrents.Torrent_720p?.Url != null ||
                                                         episode.Torrents.Torrent_1080p?.Url != null))
                    {
                        magnetUri = episode.Torrents.Torrent_720p?.Url ?? episode.Torrents.Torrent_1080p.Url;
                    }
                    else
                    {
                        magnetUri = episode.Torrents.Torrent_480p?.Url ?? episode.Torrents.Torrent_0.Url;
                    }

                    var magnet = new magnet_uri();
                    var error = new error_code();
                    var addParams = new add_torrent_params
                    {
                        save_path = Constants.Constants.ShowDownloads,
                    };
                    magnet.parse_magnet_uri(magnetUri, addParams, error);
                    using (var handle = session.add_torrent(addParams))
                    {
                        handle.set_upload_limit(_applicationSettingsViewModel.DownloadLimit * 1024);
                        handle.set_download_limit(_applicationSettingsViewModel.UploadLimit * 1024);

                        // We have to download sequentially, so that we're able to play the episode without waiting
                        handle.set_sequential_download(true);
                        var alreadyBuffered = false;
                        while (IsDownloadingEpisode)
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

                                if (progress >= Constants.Constants.MinimumShowBuffering && !alreadyBuffered)
                                {
                                    // Get episode file
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
                                        _episodeFilePath = filePath;
                                        alreadyBuffered = true;
                                        episode.FilePath = filePath;
                                        Messenger.Default.Send(new PlayShowEpisodeMessage(episode,
                                            _reportDownloadProgress));
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