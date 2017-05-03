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
using Popcorn.Models.Shows;
using Popcorn.Services.Shows.Show;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Show.Tabs
{
    public class FavoritesShowTabViewModel : ShowTabsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the FavoritesMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="showService">Show service</param>
        /// <param name="userService">User service</param>
        public FavoritesShowTabViewModel(IApplicationService applicationService, IShowService showService,
            IUserService userService)
            : base(applicationService, showService, userService,
                () => LocalizationProviderHelper.GetLocalizedValue<string>("FavoritesTitleTab"))
        {
            Messenger.Default.Register<ChangeFavoriteShowMessage>(
                this,
                async message =>
                {
                    await LoadShowsAsync();
                });
        }

        /// <summary>
        /// Load movies asynchronously
        /// </summary>
        public override async Task LoadShowsAsync()
        {
            var watch = Stopwatch.StartNew();
            Logger.Info(
                $"Loading shows favorite page {Page}...");
            HasLoadingFailed = false;
            try
            {
                IsLoadingShows = true;
                var imdbIds =
                    await UserService.GetFavoritesShows().ConfigureAwait(false);
                var shows = new List<ShowJson>();
                await imdbIds.ParallelForEachAsync(async imdbId =>
                {
                    var show = await ShowService.GetShowAsync(imdbId);
                    if (show != null)
                    {
                        show.IsFavorite = true;
                        shows.Add(show);
                    }
                });

                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    Shows.Clear();
                    Shows.AddRange(shows.Where(a => Genre != null
                        ? a.Genres.Contains(Genre.EnglishName)
                        : a.Genres.TrueForAll(b => true) && a.Rating.Percentage >= Rating * 10));
                    IsLoadingShows = false;
                    IsShowFound = Shows.Any();
                    CurrentNumberOfShows = Shows.Count;
                    MaxNumberOfShows = Shows.Count;
                    await UserService.SyncShowHistoryAsync(Shows).ConfigureAwait(false);
                });
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Error while loading shows favorite page {Page}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded shows favorite page {Page} in {elapsedMs} milliseconds.");
            }
        }
    }
}