using System.Collections.Async;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Popcorn.Messaging;
using Popcorn.Models.ApplicationState;
using Popcorn.Services.Genres;
using Popcorn.Services.Shows.Show;
using Popcorn.Services.User;
using Popcorn.ViewModels.Pages.Home.Genres;
using Popcorn.ViewModels.Pages.Home.Show.Search;
using Popcorn.ViewModels.Pages.Home.Show.Tabs;

namespace Popcorn.ViewModels.Pages.Home.Show
{
    public class ShowPageViewModel : ObservableObject, IPageViewModel
    {
        /// <summary>
        /// Used to interact with shows
        /// </summary>
        private readonly IShowService _showService;

        /// <summary>
        /// The user service
        /// </summary>
        private readonly IUserService _userService;

        /// <summary>
        /// <see cref="Caption"/>
        /// </summary>
        private string _caption;

        /// <summary>
        /// The selected tab
        /// </summary>
        private ShowTabsViewModel _selectedTab;

        /// <summary>
        /// Specify if a search is actually active
        /// </summary>
        private bool _isSearchActive;

        /// <summary>
        /// Command used to select the greatest shows tab
        /// </summary>
        public RelayCommand SelectGreatestTab { get; private set; }

        /// <summary>
        /// Command used to select the popular shows tab
        /// </summary>
        public RelayCommand SelectPopularTab { get; private set; }

        /// <summary>
        /// Command used to select the recent shows tab
        /// </summary>
        public RelayCommand SelectRecentTab { get; private set; }

        /// <summary>
        /// Command used to select the search shows tab
        /// </summary>
        public RelayCommand SelectSearchTab { get; private set; }

        /// <summary>
        /// Command used to select the favorites shows tab
        /// </summary>
        public RelayCommand SelectFavoritesTab { get; private set; }

        /// <summary>
        /// Manage genres
        /// </summary>
        private GenreViewModel _genreViewModel;

        /// <summary>
        /// <see cref="SelectedShowsIndexMenuTab"/>
        /// </summary>
        private int _selectedShowsIndexMenuTab;

        /// <summary>
        /// Application state
        /// </summary>
        private IApplicationService _applicationService;

        /// <summary>
        /// <see cref="Search"/>
        /// </summary>
        private SearchShowViewModel _search;

        /// <summary>
        /// The tabs
        /// </summary>
        private ObservableCollection<ShowTabsViewModel> _tabs = new ObservableCollection<ShowTabsViewModel>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="applicationService">The application service</param>
        /// <param name="showService">The show service</param>
        /// <param name="userService">The user service</param>
        /// <param name="genreService">The genre service</param>
        public ShowPageViewModel(IApplicationService applicationService, IShowService showService,
            IUserService userService, IGenreService genreService)
        {
            _showService = showService;
            _userService = userService;
            _applicationService = applicationService;
            GenreViewModel = new GenreViewModel(userService, genreService);
            RegisterCommands();
            RegisterMessages();

            Search = new SearchShowViewModel();

            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                Tabs.Add(new PopularShowTabViewModel(_applicationService, showService, userService));
                Tabs.Add(new GreatestShowTabViewModel(_applicationService, showService, userService));
                Tabs.Add(new RecentShowTabViewModel(_applicationService, showService, userService));
                Tabs.Add(new FavoritesShowTabViewModel(_applicationService, showService, userService));
                SelectedTab = Tabs.First();
                SelectedShowsIndexMenuTab = 0;
                await GenreViewModel.LoadGenresAsync().ConfigureAwait(false);
                await Tabs.ToList().ParallelForEachAsync(async tab =>
                {
                    await tab.LoadShowsAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Manage the show search
        /// </summary>
        public SearchShowViewModel Search
        {
            get => _search;
            set => Set(ref _search, value);
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<SearchShowMessage>(this,
                async message => await SearchShows(message.Filter));
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            SelectPopularTab = new RelayCommand(() =>
            {
                if (SelectedTab is PopularShowTabViewModel)
                    return;
                foreach (var popularTab in Tabs.OfType<PopularShowTabViewModel>())
                    SelectedTab = popularTab;
            });

            SelectGreatestTab = new RelayCommand(() =>
            {
                if (SelectedTab is GreatestShowTabViewModel)
                    return;
                foreach (var greatestTab in Tabs.OfType<GreatestShowTabViewModel>())
                    SelectedTab = greatestTab;
            });

            SelectSearchTab = new RelayCommand(() =>
            {
                if (SelectedTab is SearchShowTabViewModel)
                    return;
                foreach (var searchTab in Tabs.OfType<SearchShowTabViewModel>())
                    SelectedTab = searchTab;
            });

            SelectRecentTab = new RelayCommand(() =>
            {
                if (SelectedTab is RecentShowTabViewModel)
                    return;
                foreach (var recentTab in Tabs.OfType<RecentShowTabViewModel>())
                    SelectedTab = recentTab;
            });

            SelectFavoritesTab = new RelayCommand(() =>
            {
                if (SelectedTab is FavoritesShowTabViewModel)
                    return;
                foreach (var favoritesTab in Tabs.OfType<FavoritesShowTabViewModel>())
                    SelectedTab = favoritesTab;
            });
        }

        /// <summary>
        /// Specify if a show search is active
        /// </summary>
        public bool IsSearchActive
        {
            get => _isSearchActive;
            private set { Set(() => IsSearchActive, ref _isSearchActive, value); }
        }

        /// <summary>
        /// Manage genres
        /// </summary>
        public GenreViewModel GenreViewModel
        {
            get => _genreViewModel;
            set { Set(() => GenreViewModel, ref _genreViewModel, value); }
        }

        /// <summary>
        /// Selected index for movies menu
        /// </summary>
        public int SelectedShowsIndexMenuTab
        {
            get => _selectedShowsIndexMenuTab;
            set { Set(() => SelectedShowsIndexMenuTab, ref _selectedShowsIndexMenuTab, value); }
        }

        /// <summary>
        /// The selected tab
        /// </summary>
        public ShowTabsViewModel SelectedTab
        {
            get => _selectedTab;
            set { Set(() => SelectedTab, ref _selectedTab, value); }
        }

        /// <summary>
        /// Tabs shown into the interface
        /// </summary>
        public ObservableCollection<ShowTabsViewModel> Tabs
        {
            get => _tabs;
            set { Set(() => Tabs, ref _tabs, value); }
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
        /// Application state
        /// </summary>
        public IApplicationService ApplicationService
        {
            get => _applicationService;
            set { Set(() => ApplicationService, ref _applicationService, value); }
        }

        /// <summary>
        /// Search for show with a criteria
        /// </summary>
        /// <param name="criteria">The criteria used for search</param>
        private async Task SearchShows(string criteria)
        {
            if (string.IsNullOrEmpty(criteria))
            {
                // The search filter is empty. We have to find the search tab if any
                foreach (var searchTabToRemove in Tabs.OfType<SearchShowTabViewModel>())
                {
                    // The search tab is currently selected in the UI, we have to pick a different selected tab prior deleting
                    if (searchTabToRemove == SelectedTab)
                        SelectedTab = Tabs.FirstOrDefault();

                    Tabs.Remove(searchTabToRemove);
                    searchTabToRemove.Cleanup();
                    IsSearchActive = false;
                    SelectedShowsIndexMenuTab = 0;
                    return;
                }
            }
            else
            {
                IsSearchActive = true;
                SelectedShowsIndexMenuTab = 3;
                foreach (var searchTab in Tabs.OfType<SearchShowTabViewModel>())
                {
                    await searchTab.SearchShowsAsync(criteria);
                    if (SelectedTab != searchTab)
                        SelectedTab = searchTab;

                    return;
                }

                Tabs.Add(new SearchShowTabViewModel(ApplicationService, _showService, _userService));
                SelectedTab = Tabs.Last();
                var searchMovieTab = SelectedTab as SearchShowTabViewModel;
                if (searchMovieTab != null)
                    await searchMovieTab.SearchShowsAsync(criteria);
            }
        }
    }
}