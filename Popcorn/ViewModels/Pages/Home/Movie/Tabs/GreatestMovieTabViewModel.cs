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
    /// The greatest movies tab
    /// </summary>
    public sealed class GreatestMovieTabViewModel : MovieTabsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the GreatestMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="movieService">Movie service</param>
        /// <param name="userService">Movie history service</param>
        public GreatestMovieTabViewModel(IApplicationService applicationService, IMovieService movieService,
            IUserService userService)
            : base(applicationService, movieService, userService,
                () => LocalizationProviderHelper.GetLocalizedValue<string>("GreatestTitleTab"))
        {
        }

        /// <summary>
        /// Load movies asynchronously
        /// </summary>
        public override async Task LoadMoviesAsync()
        {
            var watch = Stopwatch.StartNew();
            Page++;
            if (Page > 1 && Movies.Count == MaxNumberOfMovies) return;
            Logger.Info(
                $"Loading movies greatest page {Page}...");
            HasLoadingFailed = false;
            try
            {
                IsLoadingMovies = true;
                var result =
                    await MovieService.GetMoviesAsync(Page,
                        MaxMoviesPerPage,
                        Rating,
                        "download_count",
                        CancellationLoadingMovies.Token,
                        Genre).ConfigureAwait(false);

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
                    $"Error while loading movies greatest page {Page}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded movies greatest page {Page} in {elapsedMs} milliseconds.");
            }
        }
    }
}