using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using NLog;
using Popcorn.Helpers;
using Popcorn.Models.ApplicationState;
using Popcorn.Models.Genre;
using Popcorn.Models.Shows;
using Popcorn.Services.Shows.Show;

namespace Popcorn.ViewModels.Pages.Home.Show.Tabs
{
    /// <summary>
    /// Manage tab controls
    /// </summary>
    public class ShowTabsViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The genre used to filter shows
        /// </summary>
        private static GenreJson _genre;

        /// <summary>
        /// The rating used to filter shows
        /// </summary>
        private static double _rating;

        /// <summary>
        /// Services used to interact with shows
        /// </summary>
        protected readonly IShowService ShowService;

        /// <summary>
        /// The current number of shows of the tab
        /// </summary>
        private int _currentNumberOfShows;

        /// <summary>
        /// Specify if a show loading has failed
        /// </summary>
        private bool _hasLoadingFailed;

        /// <summary>
        /// Specify if shows are loading
        /// </summary>
        private bool _isLoadingShows;

        /// <summary>
        /// Indicates if there's any show found
        /// </summary>
        private bool _isShowsFound = true;

        /// <summary>
        /// The maximum number of shows found
        /// </summary>
        private int _maxNumberOfShows;

        /// <summary>
        /// The tab's shows
        /// </summary>
        private ObservableCollection<ShowJson> _shows = new ObservableCollection<ShowJson>();

        /// <summary>
        /// The tab's name
        /// </summary>
        private string _tabName;

        /// <summary>
        /// Initializes a new instance of the ShowTabsViewModel class.
        /// </summary>
        /// <param name="applicationService">The application state</param>
        /// <param name="showService">Used to interact with shows</param>
        protected ShowTabsViewModel(IApplicationService applicationService, IShowService showService)
        {
            ApplicationService = applicationService;
            ShowService = showService;

            RegisterMessages();
            RegisterCommands();

            MaxShowsPerPage = Constants.Constants.MaxShowsPerPage;
            CancellationLoadingShows = new CancellationTokenSource();
        }

        /// <summary>
        /// Application state
        /// </summary>
        public IApplicationService ApplicationService { get; set; }

        /// <summary>
        /// Tab's shows
        /// </summary>
        public ObservableCollection<ShowJson> Shows
        {
            get { return _shows; }
            set { Set(() => Shows, ref _shows, value); }
        }

        /// <summary>
        /// The current number of shows in the tab
        /// </summary>
        public int CurrentNumberOfShows
        {
            get { return _currentNumberOfShows; }
            set { Set(() => CurrentNumberOfShows, ref _currentNumberOfShows, value); }
        }

        /// <summary>
        /// The maximum number of shows found
        /// </summary>
        public int MaxNumberOfShows
        {
            get { return _maxNumberOfShows; }
            set { Set(() => MaxNumberOfShows, ref _maxNumberOfShows, value); }
        }

        /// <summary>
        /// The tab's name
        /// </summary>
        public string TabName
        {
            get { return _tabName; }
            set { Set(() => TabName, ref _tabName, value); }
        }

        /// <summary>
        /// Specify if shows are loading
        /// </summary>
        public bool IsLoadingShows
        {
            get { return _isLoadingShows; }
            protected set { Set(() => IsLoadingShows, ref _isLoadingShows, value); }
        }

        /// <summary>
        /// Indicates if there's any show found
        /// </summary>
        public bool IsShowFound
        {
            get { return _isShowsFound; }
            set { Set(() => IsShowFound, ref _isShowsFound, value); }
        }

        /// <summary>
        /// The rating used to filter shows
        /// </summary>
        public double Rating
        {
            get { return _rating; }
            set { Set(() => Rating, ref _rating, value, true); }
        }

        /// <summary>
        /// Command used to reload shows
        /// </summary>
        public RelayCommand ReloadShows { get; set; }

        /// <summary>
        /// Command used to set a show as favorite
        /// </summary>
        public RelayCommand<ShowJson> SetFavoriteShowCommand { get; private set; }

        /// <summary>
        /// Command used to change show's genres
        /// </summary>
        public RelayCommand<GenreJson> ChangeShowGenreCommand { get; set; }

        /// <summary>
        /// Specify if a show loading has failed
        /// </summary>
        public bool HasLoadingFailed
        {
            get { return _hasLoadingFailed; }
            set { Set(() => HasLoadingFailed, ref _hasLoadingFailed, value); }
        }

        /// <summary>
        /// The genre used to filter shows
        /// </summary>
        protected GenreJson Genre
        {
            get { return _genre; }
            private set { Set(() => Genre, ref _genre, value, true); }
        }

        /// <summary>
        /// Current page number of loaded shows
        /// </summary>
        protected int Page { get; set; }

        /// <summary>
        /// Maximum shows number to load per page request
        /// </summary>
        protected int MaxShowsPerPage { get; }

        /// <summary>
        /// Token to cancel show loading
        /// </summary>
        protected CancellationTokenSource CancellationLoadingShows { get; private set; }

        /// <summary>
        /// Load shows asynchronously
        /// </summary>
        public virtual Task LoadShowsAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public override void Cleanup()
        {
            StopLoadingShows();
            base.Cleanup();
        }

        /// <summary>
        /// Cancel the loading of the next page
        /// </summary>
        protected void StopLoadingShows()
        {
            Logger.Info(
                "Stop loading shows.");

            CancellationLoadingShows.Cancel(true);
            CancellationLoadingShows = new CancellationTokenSource();
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {

        }

        /// <summary>
        /// Register commands
        /// </summary>
        /// <returns></returns>
        private void RegisterCommands()
        {

        }
    }
}