using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using NuGet;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.ApplicationState;
using Popcorn.Models.Genres;
using Popcorn.Services.Movies.History;
using Popcorn.Services.Movies.Movie;

namespace Popcorn.ViewModels.Pages.Home.Movie.Tabs
{
    /// <summary>
    /// The search movies tab
    /// </summary>
    public sealed class SearchMovieTabViewModel : MovieTabsViewModel
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the SearchMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="movieService">Movie service</param>
        /// <param name="movieHistoryService">Movie history service</param>
        public SearchMovieTabViewModel(IApplicationService applicationService, IMovieService movieService,
            IMovieHistoryService movieHistoryService)
            : base(applicationService, movieService, movieHistoryService)
        {
            RegisterMessages();
            RegisterCommands();
            TabName = LocalizationProviderHelper.GetLocalizedValue<string>("SearchMovieTitleTab");
        }

        /// <summary>
        /// The search filter
        /// </summary>
        public string SearchFilter { get; private set; }

        /// <summary>
        /// Search movies asynchronously
        /// </summary>
        /// <param name="searchFilter">The parameter of the search</param>
        public async Task SearchMoviesAsync(string searchFilter)
        {
            if (SearchFilter != searchFilter)
            {
                // We start an other search
                StopLoadingMovies();
                Movies.Clear();
                Page = 0;
                CurrentNumberOfMovies = 0;
                MaxNumberOfMovies = 0;
                IsLoadingMovies = false;
            }

            var watch = Stopwatch.StartNew();

            Page++;

            if (Page > 1 && Movies.Count == MaxNumberOfMovies) return;

            Logger.Info(
                $"Loading page {Page} with criteria: {searchFilter}");

            HasLoadingFailed = false;

            try
            {
                SearchFilter = searchFilter;

                IsLoadingMovies = true;

                var movies =
                    await MovieService.SearchMoviesAsync(searchFilter,
                        Page,
                        MaxMoviesPerPage,
                        Genre,
                        Rating,
                        CancellationLoadingMovies.Token).ConfigureAwait(false);

                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var moviesList = movies.Item1.ToList();
                    Movies.AddRange(moviesList);
                    IsLoadingMovies = false;
                    IsMovieFound = Movies.Any();
                    CurrentNumberOfMovies = Movies.Count;
                    MaxNumberOfMovies = movies.Item2;
                    await MovieHistoryService.SetMovieHistoryAsync(movies.Item1).ConfigureAwait(false);
                });
            }
            catch (Exception exception)
            {
                Page--;
                Logger.Error(
                    $"Error while loading page {Page} with criteria {searchFilter}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded page {Page} with criteria {searchFilter} in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<ChangeLanguageMessage>(
                this,
                language => TabName = LocalizationProviderHelper.GetLocalizedValue<string>("SearchMovieTitleTab"));

            Messenger.Default.Register<PropertyChangedMessage<GenreJson>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Genre) && Genre.Equals(e.NewValue)) return;
                StopLoadingMovies();
                Page = 0;
                Movies.Clear();
                await SearchMoviesAsync(SearchFilter);
            });

            Messenger.Default.Register<PropertyChangedMessage<double>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Rating) && Rating.Equals(e.NewValue)) return;
                StopLoadingMovies();
                Page = 0;
                Movies.Clear();
                await SearchMoviesAsync(SearchFilter);
            });
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            ReloadMovies = new RelayCommand(async () =>
            {
                ApplicationService.IsConnectionInError = false;
                StopLoadingMovies();
                await SearchMoviesAsync(SearchFilter);
            });
        }
    }
}