using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Episode;
using Popcorn.Services.Subtitles;
using Popcorn.Utils;
using Popcorn.ViewModels.Windows.Settings;
using GalaSoft.MvvmLight.Ioc;
using Popcorn.Services.Download;

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
        /// The download service
        /// </summary>
        private readonly IDownloadService<EpisodeShowJson> _downloadService;

        /// <summary>
        /// Token to cancel the download
        /// </summary>
        private CancellationTokenSource _cancellationDownloadingEpisode;

        /// <summary>
        /// Specify if an episode is downloading
        /// </summary>
        private bool _isDownloadingEpisode;

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
        /// Constructor
        /// </summary>
        /// <param name="downloadService">The download service</param>
        /// <param name="subtitlesService">The subtitles service</param>
        public DownloadShowViewModel(IDownloadService<EpisodeShowJson> downloadService,
            ISubtitlesService subtitlesService)
        {
            _downloadService = downloadService;
            _subtitlesService = subtitlesService;
            _cancellationDownloadingEpisode = new CancellationTokenSource();
            RegisterCommands();
            RegisterMessages();
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
        private void StopDownloadingEpisode()
        {
            Logger.Info(
                $"Stop downloading the episode {Episode.Title}");

            IsDownloadingEpisode = false;
            _cancellationDownloadingEpisode.Cancel(true);
            _cancellationDownloadingEpisode = new CancellationTokenSource();

            if (!string.IsNullOrEmpty(Episode?.FilePath))
            {
                try
                {
                    File.Delete(Episode.FilePath);
                    Episode.FilePath = string.Empty;
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
                IsDownloadingEpisode = true;
                Episode = message.Episode;
                EpisodeDownloadRate = 0d;
                EpisodeDownloadProgress = 0d;
                NbPeers = 0;
                NbSeeders = 0;
                var reportDownloadProgress = new Progress<double>(ReportEpisodeDownloadProgress);
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
                            var path = Path.Combine(Constants.Subtitles + message.Episode.ImdbId);
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
                            string magnetUri;
                            if (Episode.WatchInFullHdQuality && (Episode.Torrents.Torrent_720p?.Url != null ||
                                                                 Episode.Torrents.Torrent_1080p?.Url != null))
                            {
                                magnetUri = Episode.Torrents.Torrent_720p?.Url ?? Episode.Torrents.Torrent_1080p.Url;
                            }
                            else
                            {
                                magnetUri = Episode.Torrents.Torrent_480p?.Url ?? Episode.Torrents.Torrent_0.Url;
                            }

                            var settings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                            await _downloadService.Download(message.Episode, TorrentType.Magnet, MediaType.Show,
                                magnetUri, settings.UploadLimit, settings.DownloadLimit, reportDownloadProgress,
                                reportDownloadRate, reportNbSeeders, reportNbPeers, () => { }, () => { },
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
        }
    }
}