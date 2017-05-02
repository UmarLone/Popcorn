using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Genres;
using Popcorn.Services.Genres;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Genres
{
    public class GenreViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Language service
        /// </summary>
        private readonly IUserService _userService;

        /// <summary>
        /// Genre service
        /// </summary>
        private readonly IGenreService _genreService;

        /// <summary>
        /// Used to cancel loading genres
        /// </summary>
        private CancellationTokenSource _cancellationLoadingGenres;

        /// <summary>
        /// Movie genres
        /// </summary>
        private ObservableCollection<GenreJson> _genres = new ObservableCollection<GenreJson>();

        /// <summary>
        /// Selected genre
        /// </summary>
        private GenreJson _selectedGenre = new GenreJson();

        /// <summary>
        /// Initialize a new instance of GenresMovieViewModel class
        /// </summary>
        /// <param name="userService">The user service</param>
        /// <param name="genreService">The genre service</param>
        public GenreViewModel(IUserService userService, IGenreService genreService)
        {
            _userService = userService;
            _genreService = genreService;
            _cancellationLoadingGenres = new CancellationTokenSource();
            RegisterMessages();
        }

        /// <summary>
        /// Movie genres
        /// </summary>
        public ObservableCollection<GenreJson> Genres
        {
            get => _genres;
            set { Set(() => Genres, ref _genres, value); }
        }

        /// <summary>
        /// Selected genre
        /// </summary>
        public GenreJson SelectedGenre
        {
            get => _selectedGenre;
            set { Set(() => SelectedGenre, ref _selectedGenre, value); }
        }

        /// <summary>
        /// Load genres asynchronously
        /// </summary>
        public async Task LoadGenresAsync()
        {
            var language = await _userService.GetCurrentLanguageAsync();
            var genres =
                new ObservableCollection<GenreJson>(
                    await _genreService.GetGenresAsync(language.Culture, _cancellationLoadingGenres.Token));
            if (_cancellationLoadingGenres.IsCancellationRequested)
                return;

            genres.Insert(0, new GenreJson
            {
                Name = LocalizationProviderHelper.GetLocalizedValue<string>("AllLabel"),
                EnglishName = string.Empty
            });

            Genres = genres;
            SelectedGenre = genres.ElementAt(0);
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public override void Cleanup()
        {
            StopLoadingGenres();
            base.Cleanup();
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages() => Messenger.Default.Register<ChangeLanguageMessage>(
            this,
            message =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    StopLoadingGenres();
                    await LoadGenresAsync();
                });
            });

        /// <summary>
        /// Cancel the loading of genres
        /// </summary>
        private void StopLoadingGenres()
        {
            Logger.Debug(
                "Stop loading genres.");

            _cancellationLoadingGenres.Cancel(true);
            _cancellationLoadingGenres = new CancellationTokenSource();
        }
    }
}