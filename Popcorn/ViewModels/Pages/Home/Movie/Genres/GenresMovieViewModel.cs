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
using Popcorn.Models.Genre;
using Popcorn.Services.Movies.Movie;
using TMDbLib.Objects.General;

namespace Popcorn.ViewModels.Pages.Home.Movie.Genres
{
    public class GenresMovieViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Used to interact with movies
        /// </summary>
        private readonly IMovieService _movieService;

        /// <summary>
        /// Used to cancel loading genres
        /// </summary>
        private CancellationTokenSource _cancellationLoadingGenres;

        /// <summary>
        /// Movie genres
        /// </summary>
        private ObservableCollection<GenreJson> _movieGenres = new ObservableCollection<GenreJson>();

        /// <summary>
        /// Selected genre
        /// </summary>
        private GenreJson _selectedGenre = new GenreJson();

        /// <summary>
        /// Initialize a new instance of GenresMovieViewModel class
        /// </summary>
        /// <param name="movieService">The movie service</param>
        public GenresMovieViewModel(IMovieService movieService)
        {
            _movieService = movieService;
            _cancellationLoadingGenres = new CancellationTokenSource();
            RegisterMessages();
        }

        /// <summary>
        /// Movie genres
        /// </summary>
        public ObservableCollection<GenreJson> MovieGenres
        {
            get => _movieGenres;
            set { Set(() => MovieGenres, ref _movieGenres, value); }
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
            var genres =
                new ObservableCollection<GenreJson>(
                    await _movieService.GetGenresAsync(_cancellationLoadingGenres.Token));
            if (_cancellationLoadingGenres.IsCancellationRequested)
                return;

            genres.Insert(0, new GenreJson
            {
                TmdbGenre = new Genre
                {
                    Id = int.MaxValue,
                    Name = LocalizationProviderHelper.GetLocalizedValue<string>("AllLabel")
                },
                EnglishName = string.Empty
            });

            MovieGenres = genres;
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