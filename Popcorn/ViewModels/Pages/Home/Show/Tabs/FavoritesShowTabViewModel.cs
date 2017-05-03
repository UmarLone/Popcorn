using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using NuGet;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.ApplicationState;
using Popcorn.Models.Genres;
using Popcorn.Models.Shows;
using Popcorn.Services.Shows.Show;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Show.Tabs
{
    public class FavoritesShowTabViewModel : ShowTabsViewModel
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IShowService _showService;

        /// <summary>
        /// Initializes a new instance of the FavoritesMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="showService">Show service</param>
        /// <param name="userService">User service</param>
        public FavoritesShowTabViewModel(IApplicationService applicationService, IShowService showService,
            IUserService userService)
            : base(applicationService, showService, userService)
        {
            _showService = showService;
            RegisterMessages();
            RegisterCommands();
            TabName = LocalizationProviderHelper.GetLocalizedValue<string>("FavoritesTitleTab");
        }

        /// <summary>
        /// Load movies asynchronously
        /// </summary>
        public override async Task LoadShowsAsync()
        {
            var watch = Stopwatch.StartNew();

            Logger.Info(
                "Loading shows...");

            HasLoadingFailed = false;

            try
            {
                IsLoadingShows = true;

                var imdbIds =
                    await UserService.GetFavoritesMovies().ConfigureAwait(false);
                var shows = new List<ShowJson>();
                await imdbIds.ParallelForEachAsync(async imdbId =>
                {
                    var show = await _showService.GetShowAsync(imdbId);
                    if (show != null)
                    {
                        show.IsFavorite = true;
                        shows.Add(show);
                    }
                });

                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Shows.Clear();
                    Shows.AddRange(shows.Where(a => Genre != null
                        ? a.Genres.Contains(Genre.EnglishName)
                        : a.Genres.TrueForAll(b => true) && a.Rating.Percentage >= Rating * 10));
                    IsLoadingShows = false;
                    IsShowFound = Shows.Any();
                    CurrentNumberOfShows = Shows.Count;
                    MaxNumberOfShows = Shows.Count;
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

            Messenger.Default.Register<ChangeFavoriteShowMessage>(
                this,
                async message =>
                {
                    StopLoadingShows();
                    await LoadShowsAsync();
                });

            Messenger.Default.Register<PropertyChangedMessage<GenreJson>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Genre) && Genre.Equals(e.NewValue)) return;
                StopLoadingShows();
                await LoadShowsAsync();
            });

            Messenger.Default.Register<PropertyChangedMessage<double>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Rating) && Rating.Equals(e.NewValue)) return;
                StopLoadingShows();
                await LoadShowsAsync();
            });
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            ReloadShows = new RelayCommand(async () =>
            {
                ApplicationService.IsConnectionInError = false;
                StopLoadingShows();
                await LoadShowsAsync();
            });
        }
    }
}