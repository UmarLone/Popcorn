using System;
using System.Collections.Async;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.ApplicationState;
using Popcorn.Models.Genres;
using Popcorn.Models.Movie;
using Popcorn.Services.Movies.Movie;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Movie.Tabs
{
    /// <summary>
    /// Manage tab controls
    /// </summary>
    public class MovieTabsViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The genre used to filter movies
        /// </summary>
        private static GenreJson _genre;

        /// <summary>
        /// The rating used to filter movies
        /// </summary>
        private static double _rating;

        /// <summary>
        /// Services used to interact with movie history
        /// </summary>
        protected readonly IUserService UserService;

        /// <summary>
        /// Services used to interact with movies
        /// </summary>
        protected readonly IMovieService MovieService;

        /// <summary>
        /// The current number of movies of the tab
        /// </summary>
        private int _currentNumberOfMovies;

        /// <summary>
        /// Specify if a movie loading has failed
        /// </summary>
        private bool _hasLoadingFailed;

        /// <summary>
        /// Specify if movies are loading
        /// </summary>
        private bool _isLoadingMovies;

        /// <summary>
        /// Indicates if there's any movie found
        /// </summary>
        private bool _isMoviesFound = true;

        /// <summary>
        /// The maximum number of movies found
        /// </summary>
        private int _maxNumberOfMovies;

        /// <summary>
        /// The tab's movies
        /// </summary>
        private ObservableCollection<MovieJson> _movies = new ObservableCollection<MovieJson>();

        /// <summary>
        /// The tab's name
        /// </summary>
        private string _tabName;

        /// <summary>
        /// Func which generates the tab name
        /// </summary>
        private readonly Func<string> _tabNameGenerator;

        /// <summary>
        /// Initializes a new instance of the MovieTabsViewModel class.
        /// </summary>
        /// <param name="applicationService">The application state</param>
        /// <param name="movieService">Used to interact with movies</param>
        /// <param name="userService">Used to interact with movie history</param>
        /// <param name="tabNameGenerator">Func which generates the tab name</param>
        protected MovieTabsViewModel(IApplicationService applicationService, IMovieService movieService,
            IUserService userService, Func<string> tabNameGenerator)
        {
            ApplicationService = applicationService;
            MovieService = movieService;
            UserService = userService;

            RegisterMessages();
            RegisterCommands();

            _tabNameGenerator = tabNameGenerator;
            TabName = tabNameGenerator.Invoke();
            MaxMoviesPerPage = Utils.Constants.MaxMoviesPerPage;
            CancellationLoadingMovies = new CancellationTokenSource();
        }

        /// <summary>
        /// Application state
        /// </summary>
        public IApplicationService ApplicationService { get; }

        /// <summary>
        /// Tab's movies
        /// </summary>
        public ObservableCollection<MovieJson> Movies
        {
            get => _movies;
            set { Set(() => Movies, ref _movies, value); }
        }

        /// <summary>
        /// The current number of movies in the tab
        /// </summary>
        public int CurrentNumberOfMovies
        {
            get => _currentNumberOfMovies;
            set { Set(() => CurrentNumberOfMovies, ref _currentNumberOfMovies, value); }
        }

        /// <summary>
        /// The maximum number of movies found
        /// </summary>
        public int MaxNumberOfMovies
        {
            get => _maxNumberOfMovies;
            set { Set(() => MaxNumberOfMovies, ref _maxNumberOfMovies, value); }
        }

        /// <summary>
        /// The tab's name
        /// </summary>
        public string TabName
        {
            get => _tabName;
            set { Set(() => TabName, ref _tabName, value); }
        }

        /// <summary>
        /// Specify if movies are loading
        /// </summary>
        public bool IsLoadingMovies
        {
            get => _isLoadingMovies;
            protected set { Set(() => IsLoadingMovies, ref _isLoadingMovies, value); }
        }

        /// <summary>
        /// Indicates if there's any movie found
        /// </summary>
        public bool IsMovieFound
        {
            get => _isMoviesFound;
            set { Set(() => IsMovieFound, ref _isMoviesFound, value); }
        }

        /// <summary>
        /// The rating used to filter movies
        /// </summary>
        public double Rating
        {
            get => _rating;
            set { Set(() => Rating, ref _rating, value, true); }
        }

        /// <summary>
        /// Command used to reload movies
        /// </summary>
        public RelayCommand ReloadMovies { get; set; }

        /// <summary>
        /// Command used to set a movie as favorite
        /// </summary>
        public RelayCommand<MovieJson> SetFavoriteMovieCommand { get; private set; }

        /// <summary>
        /// Command used to change movie's genres
        /// </summary>
        public RelayCommand<GenreJson> ChangeMovieGenreCommand { get; set; }

        /// <summary>
        /// Specify if a movie loading has failed
        /// </summary>
        public bool HasLoadingFailed
        {
            get => _hasLoadingFailed;
            set { Set(() => HasLoadingFailed, ref _hasLoadingFailed, value); }
        }

        /// <summary>
        /// The genre used to filter movies
        /// </summary>
        protected GenreJson Genre
        {
            get => _genre;
            private set { Set(() => Genre, ref _genre, value, true); }
        }

        /// <summary>
        /// Current page number of loaded movies
        /// </summary>
        protected int Page { get; set; }

        /// <summary>
        /// Maximum movies number to load per page request
        /// </summary>
        protected int MaxMoviesPerPage { get; }

        /// <summary>
        /// Token to cancel movie loading
        /// </summary>
        protected CancellationTokenSource CancellationLoadingMovies { get; private set; }

        /// <summary>
        /// Load movies asynchronously
        /// </summary>
        public virtual Task LoadMoviesAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public override void Cleanup()
        {
            StopLoadingMovies();
            base.Cleanup();
        }

        /// <summary>
        /// Cancel the loading of the next page
        /// </summary>
        protected void StopLoadingMovies()
        {
            Logger.Info(
                "Stop loading movies.");

            CancellationLoadingMovies.Cancel(true);
            CancellationLoadingMovies = new CancellationTokenSource();
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<ChangeLanguageMessage>(
                this,
                async message =>
                {
                    var movies = Movies.ToList();
                    await movies.ParallelForEachAsync(async movie =>
                    {
                        await MovieService.TranslateMovieAsync(movie).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                });

            Messenger.Default.Register<ChangeLanguageMessage>(
                this,
                language => TabName = _tabNameGenerator.Invoke());

            Messenger.Default.Register<PropertyChangedMessage<GenreJson>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Genre) && Genre.Equals(e.NewValue)) return;
                StopLoadingMovies();
                await LoadMoviesAsync().ConfigureAwait(false);
            });

            Messenger.Default.Register<PropertyChangedMessage<double>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Rating) && Rating.Equals(e.NewValue)) return;
                StopLoadingMovies();
                await LoadMoviesAsync().ConfigureAwait(false);
            });

            Messenger.Default.Register<ChangeFavoriteMovieMessage>(
                this,
                async message => await UserService.SyncMovieHistoryAsync(Movies).ConfigureAwait(false));
        }

        /// <summary>
        /// Register commands
        /// </summary>
        /// <returns></returns>
        private void RegisterCommands()
        {
            ReloadMovies = new RelayCommand(async () =>
            {
                ApplicationService.IsConnectionInError = false;
                StopLoadingMovies();
                await LoadMoviesAsync().ConfigureAwait(false);
            });

            SetFavoriteMovieCommand =
                new RelayCommand<MovieJson>(async movie =>
                {
                    await UserService.SetMovieAsync(movie).ConfigureAwait(false);
                    Messenger.Default.Send(new ChangeFavoriteMovieMessage());
                });

            ChangeMovieGenreCommand =
                new RelayCommand<GenreJson>(genre => Genre = genre.Name ==
                                                             LocalizationProviderHelper.GetLocalizedValue<string>(
                                                                 "AllLabel")
                    ? null
                    : genre);
        }
    }
}