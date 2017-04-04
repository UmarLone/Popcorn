using System.Collections.Async;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Popcorn.Messaging;
using Popcorn.Models.ApplicationState;
using Popcorn.Services.Movies.History;
using Popcorn.Services.Movies.Movie;
using Popcorn.ViewModels.Pages.Home.Movie.Genres;
using Popcorn.ViewModels.Pages.Home.Movie.Search;
using Popcorn.ViewModels.Pages.Home.Movie.Tabs;

namespace Popcorn.ViewModels.Pages.Home.Movie
{
    public class MoviePageViewModel : ObservableObject, IPageViewModel
    {
        /// <summary>
        /// <see cref="Caption"/>
        /// </summary>
        private string _caption;

        /// <summary>
        /// Used to interact with movie history
        /// </summary>
        private readonly IMovieHistoryService _movieHistoryService;

        /// <summary>
        /// Used to interact with movies
        /// </summary>
        private readonly IMovieService _movieService;

        /// <summary>
        /// Application state
        /// </summary>
        private IApplicationService _applicationService;

        /// <summary>
        /// Manage movie's genres
        /// </summary>
        private GenresMovieViewModel _genresMovie;

        /// <summary>
        /// Specify if a search is actually active
        /// </summary>
        private bool _isMovieSearchActive;

        /// <summary>
        /// <see cref="SelectedMoviesIndexMenuTab"/>
        /// </summary>
        private int _selectedMoviesIndexMenuTab;

        /// <summary>
        /// The selected tab
        /// </summary>
        private MovieTabsViewModel _selectedTab;

        /// <summary>
        /// <see cref="Search"/>
        /// </summary>
        private SearchMovieViewModel _search;

        /// <summary>
        /// The tabs
        /// </summary>
        private ObservableCollection<MovieTabsViewModel> _tabs = new ObservableCollection<MovieTabsViewModel>();

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        /// <param name="movieService">Instance of MovieService</param>
        /// <param name="movieHistoryService">Instance of MovieHistoryService</param>
        /// <param name="applicationService">Instance of ApplicationService</param>
        public MoviePageViewModel(IMovieService movieService,
            IMovieHistoryService movieHistoryService, IApplicationService applicationService)
        {
            _movieService = movieService;
            _movieHistoryService = movieHistoryService;
            ApplicationService = applicationService;
            GenresMovie = new GenresMovieViewModel(_movieService);
            RegisterMessages();
            RegisterCommands();

            Search = new SearchMovieViewModel();

            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                Tabs.Add(new PopularMovieTabViewModel(ApplicationService, _movieService, _movieHistoryService));
                Tabs.Add(new GreatestMovieTabViewModel(ApplicationService, _movieService, _movieHistoryService));
                Tabs.Add(new RecentMovieTabViewModel(ApplicationService, _movieService, _movieHistoryService));
                Tabs.Add(new FavoritesMovieTabViewModel(ApplicationService, _movieService, _movieHistoryService));
                Tabs.Add(new SeenMovieTabViewModel(ApplicationService, _movieService, _movieHistoryService));
                SelectedTab = Tabs.First();
                SelectedMoviesIndexMenuTab = 0;
                var loadMoviesTask = Tabs.ParallelForEachAsync(async tab =>
                {
                    await tab.LoadMoviesAsync();
                });

                var loadGenreTask = GenresMovie.LoadGenresAsync();

                await Task.WhenAll(new List<Task>
                {
                    loadMoviesTask,
                    loadGenreTask
                });
            });
        }

        /// <summary>
        /// Manage the movie search
        /// </summary>
        public SearchMovieViewModel Search
        {
            get => _search;
            set => Set(ref _search, value);
        }

        /// <summary>
        /// Tab caption 
        /// </summary>
        public string Caption
        {
            get => _caption;
            set => Set(ref _caption, value);
        }

        /// <summary>
        /// Specify if a movie search is active
        /// </summary>
        public bool IsMovieSearchActive
        {
            get => _isMovieSearchActive;
            private set { Set(() => IsMovieSearchActive, ref _isMovieSearchActive, value); }
        }

        /// <summary>
        /// Tabs shown into the interface
        /// </summary>
        public ObservableCollection<MovieTabsViewModel> Tabs
        {
            get => _tabs;
            set { Set(() => Tabs, ref _tabs, value); }
        }

        /// <summary>
        /// The selected tab
        /// </summary>
        public MovieTabsViewModel SelectedTab
        {
            get => _selectedTab;
            set { Set(() => SelectedTab, ref _selectedTab, value); }
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<SearchMovieMessage>(this,
                async message => await SearchMovies(message.Filter));
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            SelectGreatestTab = new RelayCommand(() =>
            {
                if (SelectedTab is GreatestMovieTabViewModel)
                    return;
                foreach (var greatestTab in Tabs.OfType<GreatestMovieTabViewModel>())
                    SelectedTab = greatestTab;
            });

            SelectPopularTab = new RelayCommand(() =>
            {
                if (SelectedTab is PopularMovieTabViewModel)
                    return;
                foreach (var popularTab in Tabs.OfType<PopularMovieTabViewModel>())
                    SelectedTab = popularTab;
            });

            SelectRecentTab = new RelayCommand(() =>
            {
                if (SelectedTab is RecentMovieTabViewModel)
                    return;
                foreach (var recentTab in Tabs.OfType<RecentMovieTabViewModel>())
                    SelectedTab = recentTab;
            });

            SelectSearchTab = new RelayCommand(() =>
            {
                if (SelectedTab is SearchMovieTabViewModel)
                    return;
                foreach (var searchTab in Tabs.OfType<SearchMovieTabViewModel>())
                    SelectedTab = searchTab;
            });

            SelectFavoritesTab = new RelayCommand(() =>
            {
                if (SelectedTab is FavoritesMovieTabViewModel)
                    return;
                foreach (var favoritesTab in Tabs.OfType<FavoritesMovieTabViewModel>())
                    SelectedTab = favoritesTab;
            });

            SelectSeenTab = new RelayCommand(() =>
            {
                if (SelectedTab is SeenMovieTabViewModel)
                    return;
                foreach (var seenTab in Tabs.OfType<SeenMovieTabViewModel>())
                    SelectedTab = seenTab;
            });
        }

        /// <summary>
        /// Manage movie's genres
        /// </summary>
        public GenresMovieViewModel GenresMovie
        {
            get => _genresMovie;
            set { Set(() => GenresMovie, ref _genresMovie, value); }
        }

        /// <summary>
        /// Application state
        /// </summary>
        public IApplicationService ApplicationService
        {
            get => _applicationService;
            set { Set(() => ApplicationService, ref _applicationService, value); }
        }

        /// <summary>
        /// Command used to select the greatest movies tab
        /// </summary>
        public RelayCommand SelectGreatestTab { get; private set; }

        /// <summary>
        /// Command used to select the popular movies tab
        /// </summary>
        public RelayCommand SelectPopularTab { get; private set; }

        /// <summary>
        /// Command used to select the recent movies tab
        /// </summary>
        public RelayCommand SelectRecentTab { get; private set; }

        /// <summary>
        /// Command used to select the search movies tab
        /// </summary>
        public RelayCommand SelectSearchTab { get; private set; }

        /// <summary>
        /// Command used to select the seen movies tab
        /// </summary>
        public RelayCommand SelectSeenTab { get; private set; }

        /// <summary>
        /// Command used to select the favorites movies tab
        /// </summary>
        public RelayCommand SelectFavoritesTab { get; private set; }

        /// <summary>
        /// Selected index for movies menu
        /// </summary>
        public int SelectedMoviesIndexMenuTab
        {
            get => _selectedMoviesIndexMenuTab;
            set { Set(() => SelectedMoviesIndexMenuTab, ref _selectedMoviesIndexMenuTab, value); }
        }

        /// <summary>
        /// Search for movie with a criteria
        /// </summary>
        /// <param name="criteria">The criteria used for search</param>
        private async Task SearchMovies(string criteria)
        {
            if (string.IsNullOrEmpty(criteria))
            {
                // The search filter is empty. We have to find the search tab if any
                foreach (var searchTabToRemove in Tabs.OfType<SearchMovieTabViewModel>())
                {
                    // The search tab is currently selected in the UI, we have to pick a different selected tab prior deleting
                    if (searchTabToRemove == SelectedTab)
                        SelectedTab = Tabs.FirstOrDefault();

                    Tabs.Remove(searchTabToRemove);
                    searchTabToRemove.Cleanup();
                    IsMovieSearchActive = false;
                    SelectedMoviesIndexMenuTab = 0;
                    return;
                }
            }
            else
            {
                IsMovieSearchActive = true;
                SelectedMoviesIndexMenuTab = 3;
                foreach (var searchTab in Tabs.OfType<SearchMovieTabViewModel>())
                {
                    await searchTab.SearchMoviesAsync(criteria);
                    if (SelectedTab != searchTab)
                        SelectedTab = searchTab;

                    return;
                }

                Tabs.Add(new SearchMovieTabViewModel(ApplicationService, _movieService, _movieHistoryService));
                SelectedTab = Tabs.Last();
                var searchMovieTab = SelectedTab as SearchMovieTabViewModel;
                if (searchMovieTab != null)
                    await searchMovieTab.SearchMoviesAsync(criteria);
            }
        }
    }
}
