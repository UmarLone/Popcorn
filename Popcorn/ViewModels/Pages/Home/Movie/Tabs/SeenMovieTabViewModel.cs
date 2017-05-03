using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NuGet;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.ApplicationState;
using Popcorn.Models.Movie;
using Popcorn.Services.Movies.Movie;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Movie.Tabs
{
    public class SeenMovieTabViewModel : MovieTabsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the SeenMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="movieService">Movie service</param>
        /// <param name="userService">Movie history service</param>
        public SeenMovieTabViewModel(IApplicationService applicationService, IMovieService movieService,
            IUserService userService)
            : base(applicationService, movieService, userService,
                () => LocalizationProviderHelper.GetLocalizedValue<string>("SeenTitleTab"))
        {
            Messenger.Default.Register<ChangeSeenMovieMessage>(
                this,
                async message =>
                {
                    await LoadMoviesAsync();
                });
        }

        /// <summary>
        /// Load movies asynchronously
        /// </summary>
        public override async Task LoadMoviesAsync()
        {
            var watch = Stopwatch.StartNew();
            Logger.Info(
                $"Loading movies seen page {Page}...");
            HasLoadingFailed = false;
            try
            {
                IsLoadingMovies = true;
                var imdbIds =
                    await UserService.GetSeenMovies().ConfigureAwait(false);
                var movies = new List<MovieJson>();
                await imdbIds.ParallelForEachAsync(async imdbId =>
                {
                    var movie = await MovieService.GetMovieAsync(imdbId);
                    if (movie != null)
                    {
                        movie.HasBeenSeen = true;
                        movies.Add(movie);
                    }
                });

                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    Movies.Clear();
                    Movies.AddRange(movies.Where(a => Genre != null
                        ? a.Genres.Contains(Genre.EnglishName)
                        : a.Genres.TrueForAll(b => true) && a.Rating >= Rating));
                    IsLoadingMovies = false;
                    IsMovieFound = Movies.Any();
                    CurrentNumberOfMovies = Movies.Count;
                    MaxNumberOfMovies = Movies.Count;
                    await UserService.SyncMovieHistoryAsync(Movies).ConfigureAwait(false);
                });
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Error while loading movies seen page {Page}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded movies seen page {Page} in {elapsedMs} milliseconds.");
            }
        }
    }
}