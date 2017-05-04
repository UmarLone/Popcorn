using System.Diagnostics;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Messaging;
using Popcorn.Models.Shows;
using Popcorn.ViewModels.Pages.Home.Show.Download;
using Popcorn.Services.Subtitles;
using Popcorn.Services.Download;
using Popcorn.Models.Episode;

namespace Popcorn.ViewModels.Pages.Home.Show.Details
{
    public class ShowDetailsViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; }= LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The show
        /// </summary>
        private ShowJson _show;

        /// <summary>
        /// Specify if a trailer is playing
        /// </summary>
        private bool _isPlayingTrailer;

        /// <summary>
        /// Specify if a trailer is loading
        /// </summary>
        private bool _isTrailerLoading;

        /// <summary>
        /// Torrent health, from 0 to 10
        /// </summary>
        private double _torrentHealth;

        /// <summary>
        /// The download show view model instance
        /// </summary>
        private DownloadShowViewModel _downloadShowViewModel;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="subtitlesService">The subtitles service</param>
        public ShowDetailsViewModel(ISubtitlesService subtitlesService)
        {
            RegisterCommands();
            RegisterMessages();
            var downloadService = new DownloadShowService<EpisodeShowJson>();
            DownloadShow = new DownloadShowViewModel(downloadService, subtitlesService);
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            LoadShowCommand = new RelayCommand<ShowJson>(LoadShow);
        }

        /// <summary>
        /// Specify if a trailer is loading
        /// </summary>
        public bool IsTrailerLoading
        {
            get => _isTrailerLoading;
            set { Set(() => IsTrailerLoading, ref _isTrailerLoading, value); }
        }

        /// <summary>
        /// Specify if a trailer is playing
        /// </summary>
        public bool IsPlayingTrailer
        {
            get => _isPlayingTrailer;
            set { Set(() => IsPlayingTrailer, ref _isPlayingTrailer, value); }
        }

        /// <summary>
        /// Torrent health, from 0 to 10
        /// </summary>
        public double TorrentHealth
        {
            get => _torrentHealth;
            set { Set(() => TorrentHealth, ref _torrentHealth, value); }
        }

        /// <summary>
        /// Command used to load the movie
        /// </summary>
        public RelayCommand<ShowJson> LoadShowCommand { get; private set; }

        /// <summary>
        /// The show
        /// </summary>
        public ShowJson Show
        {
            get => _show;
            set => Set(ref _show, value);
        }

        /// <summary>
        /// The download show view model instance
        /// </summary>
        public DownloadShowViewModel DownloadShow
        {
            get => _downloadShowViewModel;
            set => Set(ref _downloadShowViewModel, value);
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages() => Messenger.Default.Register<StopPlayingEpisodeMessage>(
            this,
            message =>
            {
                DownloadShow.IsDownloadingEpisode = false;
            });

        /// <summary>
        /// Load the requested show
        /// </summary>
        /// <param name="show">The show to load</param>
        private void LoadShow(ShowJson show)
        {
            var watch = Stopwatch.StartNew();

            Messenger.Default.Send(new LoadShowMessage());
            Show = show;
            foreach (var episode in Show.Episodes)
            {
                episode.ImdbId = Show.ImdbId;
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Logger.Debug($"LoadShow ({show.ImdbId}) in {elapsedMs} milliseconds.");
        }
    }
}