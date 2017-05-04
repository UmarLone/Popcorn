using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Episode;
using Popcorn.Models.Subtitles;
using Popcorn.Services.Subtitles;

namespace Popcorn.Controls.Show
{
    /// <summary>
    /// Logique d'interaction pour EpisodeDetail.xaml
    /// </summary>
    public partial class EpisodeDetail : INotifyPropertyChanged
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The subtitles service
        /// </summary>
        private readonly ISubtitlesService _subtitlesService;

        /// <summary>
        /// Play an episode
        /// </summary>
        private ICommand _playCommand;

        /// <summary>
        /// True if subtitles are loading
        /// </summary>
        private bool _loadingSubtitles;

        /// <summary>
        /// Selected episode
        /// </summary>
        public static readonly DependencyProperty EpisodeProperty =
            DependencyProperty.Register("Episode",
                typeof(EpisodeShowJson), typeof(EpisodeDetail),
                new PropertyMetadata(null, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var detail = dependencyObject as EpisodeDetail;
            var episode = detail?.Episode;
            if (episode == null) return;

            detail.Title.Text = episode.Title;
            detail.SeasonNumber.Text = $"Season {episode.Season}";
            detail.EpisodeNumber.Text = $"Episode {episode.EpisodeNumber}";
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var date = dtDateTime.AddSeconds(episode.FirstAired).ToLocalTime();
            detail.Duration.Text = $"Released {date.ToShortDateString()}";
            detail.Synopsis.Text = episode.Overview;
            Task.Run(async () =>
            {
                await detail.LoadSubtitles(episode);
            });
        }

        /// <summary>
        /// True if subtitles are loading
        /// </summary>
        public bool LoadingSubtitles
        {
            get => _loadingSubtitles;
            set
            {
                _loadingSubtitles = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The selected episode
        /// </summary>
        public EpisodeShowJson Episode
        {
            get => (EpisodeShowJson) GetValue(EpisodeProperty);
            set => SetValue(EpisodeProperty, value);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public EpisodeDetail()
        {
            InitializeComponent();
            _subtitlesService = SimpleIoc.Default.GetInstance<ISubtitlesService>();
            PlayCommand = new RelayCommand(() =>
            {
                Messenger.Default.Send(new DownloadShowEpisodeMessage(Episode));
            });
        }

        /// <summary>
        /// Play an episode
        /// </summary>
        public ICommand PlayCommand
        {
            get => _playCommand;
            set
            {
                _playCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Load the episode's subtitles asynchronously
        /// </summary>
        /// <param name="episode">The episode</param>
        private async Task LoadSubtitles(EpisodeShowJson episode)
        {
            Logger.Debug(
                $"Load subtitles for episode: {episode.Title}");
            LoadingSubtitles = true;
            try
            {
                var languages = (await _subtitlesService.GetSubLanguages()).ToList();
                if (int.TryParse(new string(episode.ImdbId
                    .SkipWhile(x => !char.IsDigit(x))
                    .TakeWhile(char.IsDigit)
                    .ToArray()), out int imdbId))
                {
                    var subtitles = (await _subtitlesService.SearchSubtitlesFromImdb(
                            languages.Select(lang => lang.SubLanguageID).Aggregate((a, b) => a + "," + b),
                            imdbId.ToString()))
                        .Where(a => a.MovieName.ToLowerInvariant().Contains(episode.Title.ToLowerInvariant()));

                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        episode.AvailableSubtitles =
                            new ObservableCollection<Subtitle>(subtitles.OrderBy(a => a.LanguageName)
                                .Select(sub => new Subtitle
                                {
                                    Sub = sub
                                })
                                .GroupBy(x => x.Sub.LanguageName,
                                    (k, g) =>
                                        g.Aggregate(
                                            (a, x) =>
                                                (Convert.ToDouble(x.Sub.Rating, CultureInfo.InvariantCulture) >=
                                                 Convert.ToDouble(a.Sub.Rating, CultureInfo.InvariantCulture))
                                                    ? x
                                                    : a)));
                        episode.AvailableSubtitles.Insert(0, new Subtitle
                        {
                            Sub = new OSDB.Subtitle
                            {
                                LanguageName = LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel")
                            }
                        });

                        episode.AvailableSubtitles.Insert(1, new Subtitle
                        {
                            Sub = new OSDB.Subtitle
                            {
                                LanguageName = LocalizationProviderHelper.GetLocalizedValue<string>("CustomLabel")
                            }
                        });

                        episode.SelectedSubtitle = episode.AvailableSubtitles.FirstOrDefault();
                    });

                    LoadingSubtitles = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed loading subtitles for : {episode.Title}. {ex.Message}");
                LoadingSubtitles = false;
            }
        }

        /// <summary>
        /// Implementation of <see cref="INotifyPropertyChanged"/>
        /// </summary>
        /// <param name="propertyName"></param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Event of <see cref="INotifyPropertyChanged"/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}