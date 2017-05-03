using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NuGet;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.ApplicationState;
using Popcorn.Services.Movies.Movie;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Movie.Tabs
{
    /// <summary>
    /// The search movies tab
    /// </summary>
    public sealed class SearchMovieTabViewModel : MovieTabsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the SearchMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="movieService">Movie service</param>
        /// <param name="userService">Movie history service</param>
        public SearchMovieTabViewModel(IApplicationService applicationService, IMovieService movieService,
            IUserService userService)
            : base(applicationService, movieService, userService,
                () => LocalizationProviderHelper.GetLocalizedValue<string>("SearchTitleTab"))
        {
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
            var watch = Stopwatch.StartNew();
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

            Page++;
            if (Page > 1 && Movies.Count == MaxNumberOfMovies) return;
            Logger.Info(
                $"Loading movies search page {Page} with criteria: {searchFilter}");
            HasLoadingFailed = false;
            try
            {
                SearchFilter = searchFilter;
                IsLoadingMovies = true;
                var result =
                    await MovieService.SearchMoviesAsync(searchFilter,
                            Page,
                            MaxMoviesPerPage,
                            Genre,
                            Rating,
                            CancellationLoadingMovies.Token)
                        .ConfigureAwait(false);

                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    Movies.AddRange(result.movies);
                    IsLoadingMovies = false;
                    IsMovieFound = Movies.Any();
                    CurrentNumberOfMovies = Movies.Count;
                    MaxNumberOfMovies = result.nbMovies;
                    await UserService.SyncMovieHistoryAsync(Movies).ConfigureAwait(false);
                });
            }
            catch (Exception exception)
            {
                Page--;
                Logger.Error(
                    $"Error while loading movies search page {Page} with criteria {searchFilter}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded movies search page {Page} with criteria {searchFilter} in {elapsedMs} milliseconds.");
            }
        }
    }
}