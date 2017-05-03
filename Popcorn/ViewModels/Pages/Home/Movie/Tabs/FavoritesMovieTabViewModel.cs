using System;
using System.Collections.Async;
using System.Collections.Generic;
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
using Popcorn.Models.Movie;
using Popcorn.Services.Movies.Movie;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Movie.Tabs
{
    public class FavoritesMovieTabViewModel : MovieTabsViewModel
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IMovieService _movieService;

        /// <summary>
        /// Initializes a new instance of the FavoritesMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="movieService">Movie service</param>
        /// <param name="userService">Movie history service</param>
        public FavoritesMovieTabViewModel(IApplicationService applicationService, IMovieService movieService,
            IUserService userService)
            : base(applicationService, movieService, userService)
        {
            _movieService = movieService;
            RegisterMessages();
            RegisterCommands();
            TabName = LocalizationProviderHelper.GetLocalizedValue<string>("FavoritesTitleTab");
        }

        /// <summary>
        /// Load movies asynchronously
        /// </summary>
        public override async Task LoadMoviesAsync()
        {
            var watch = Stopwatch.StartNew();

            Logger.Info(
                "Loading movies...");

            HasLoadingFailed = false;

            try
            {
                IsLoadingMovies = true;

                var imdbIds =
                    await UserService.GetFavoritesMovies().ConfigureAwait(false);
                var movies = new List<MovieJson>();
                await imdbIds.ParallelForEachAsync(async imdbId =>
                {
                    var movie = await _movieService.GetMovieAsync(imdbId);
                    if (movie != null)
                    {
                        movie.IsFavorite = true;
                        movies.Add(movie);
                    }
                });

                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Movies.Clear();
                    Movies.AddRange(movies.Where(a => Genre != null ? a.Genres.Contains(Genre.EnglishName) : a.Genres.TrueForAll(b => true) && a.Rating >= Rating));
                    IsLoadingMovies = false;
                    IsMovieFound = Movies.Any();
                    CurrentNumberOfMovies = Movies.Count;
                    MaxNumberOfMovies = Movies.Count;
                });
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Error while loading page {Page}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded movies in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<ChangeLanguageMessage>(
                this,
                language => TabName = LocalizationProviderHelper.GetLocalizedValue<string>("FavoritesTitleTab"));

            Messenger.Default.Register<ChangeFavoriteMovieMessage>(
                this,
                async message =>
                {
                    StopLoadingMovies();
                    await LoadMoviesAsync();
                });

            Messenger.Default.Register<PropertyChangedMessage<GenreJson>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Genre) && Genre.Equals(e.NewValue)) return;
                StopLoadingMovies();
                await LoadMoviesAsync();
            });

            Messenger.Default.Register<PropertyChangedMessage<double>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Rating) && Rating.Equals(e.NewValue)) return;
                StopLoadingMovies();
                await LoadMoviesAsync();
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
                await LoadMoviesAsync();
            });
        }
    }
}