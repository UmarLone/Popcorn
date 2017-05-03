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
using Popcorn.Services.Shows.Show;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Show.Tabs
{
    public class SearchShowTabViewModel : ShowTabsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the SearchMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="showService">Show service</param>
        /// <param name="userService">The user service</param>
        public SearchShowTabViewModel(IApplicationService applicationService, IShowService showService,
            IUserService userService)
            : base(applicationService, showService, userService,
                () => LocalizationProviderHelper.GetLocalizedValue<string>("SearchTitleTab"))
        {
        }

        /// <summary>
        /// The search filter
        /// </summary>
        public string SearchFilter { get; private set; }

        /// <summary>
        /// Search shows asynchronously
        /// </summary>
        /// <param name="searchFilter">The parameter of the search</param>
        public async Task SearchShowsAsync(string searchFilter)
        {
            var watch = Stopwatch.StartNew();
            if (SearchFilter != searchFilter)
            {
                // We start an other search
                StopLoadingShows();
                Shows.Clear();
                Page = 0;
                CurrentNumberOfShows = 0;
                MaxNumberOfShows = 0;
                IsLoadingShows = false;
            }

            Page++;
            if (Page > 1 && Shows.Count == MaxNumberOfShows) return;
            Logger.Info(
                $"Loading shows search page {Page} with criteria: {searchFilter}");
            HasLoadingFailed = false;
            try
            {
                SearchFilter = searchFilter;
                IsLoadingShows = true;
                var result =
                    await ShowService.SearchShowsAsync(searchFilter,
                            Page,
                            MaxNumberOfShows,
                            Genre,
                            Rating * 10,
                            CancellationLoadingShows.Token)
                        .ConfigureAwait(false);

                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    Shows.AddRange(result.shows);
                    IsLoadingShows = false;
                    IsShowFound = Shows.Any();
                    CurrentNumberOfShows = Shows.Count;
                    MaxNumberOfShows = result.nbShows;
                    await UserService.SyncShowHistoryAsync(Shows).ConfigureAwait(false);
                });
            }
            catch (Exception exception)
            {
                Page--;
                Logger.Error(
                    $"Error while loading shows search page {Page} with criteria {searchFilter}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded shows search page {Page} with criteria {searchFilter} in {elapsedMs} milliseconds.");
            }
        }
    }
}