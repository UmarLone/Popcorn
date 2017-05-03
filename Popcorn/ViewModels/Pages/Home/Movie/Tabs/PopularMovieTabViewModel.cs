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
    /// The popular movies tab
    /// </summary>
    public sealed class PopularMovieTabViewModel : MovieTabsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the PopularMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="movieService">Movie service</param>
        /// <param name="userService">User service</param>
        public PopularMovieTabViewModel(IApplicationService applicationService, IMovieService movieService,
            IUserService userService)
            : base(applicationService, movieService, userService,
                () => LocalizationProviderHelper.GetLocalizedValue<string>("PopularTitleTab"))
        {
        }

        /// <summary>
        /// Load movies asynchronously
        /// </summary>
        public override async Task LoadMoviesAsync(bool reset = false)
        {
            await LoadingSemaphore.WaitAsync();
            StopLoadingMovies();
            if (reset)
            {
                Movies.Clear();
                Page = 0;
            }

            var watch = Stopwatch.StartNew();
            Page++;
            if (Page > 1 && Movies.Count == MaxNumberOfMovies)
            {
                Page--;
                LoadingSemaphore.Release();
                return;
            }

            Logger.Info(
                $"Loading movies popular page {Page}...");
            HasLoadingFailed = false;
            try
            {
                IsLoadingMovies = true;
                var result =
                    await MovieService.GetMoviesAsync(Page,
                        MaxMoviesPerPage,
                        Rating,
                        "seeds",
                        CancellationLoadingMovies.Token,
                        Genre);
                Movies.AddRange(result.movies);
                IsLoadingMovies = false;
                IsMovieFound = Movies.Any();
                CurrentNumberOfMovies = Movies.Count;
                MaxNumberOfMovies = result.nbMovies;
                await UserService.SyncMovieHistoryAsync(Movies);
            }
            catch (Exception exception)
            {
                Page--;
                Logger.Error(
                    $"Error while loading movies popular page {Page}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded movies popular page {Page} in {elapsedMs} milliseconds.");
                LoadingSemaphore.Release();
            }
        }
    }
}