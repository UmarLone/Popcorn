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
    public class PopularShowTabViewModel : ShowTabsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the PopularShowTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="showService">Show service</param>
        /// <param name="userService">The user service</param>
        public PopularShowTabViewModel(IApplicationService applicationService, IShowService showService,
            IUserService userService)
            : base(applicationService, showService, userService,
                () => LocalizationProviderHelper.GetLocalizedValue<string>("PopularTitleTab"))
        {
        }

        /// <summary>
        /// Load shows asynchronously
        /// </summary>
        public override async Task LoadShowsAsync(bool reset = false)
        {
            await LoadingSemaphore.WaitAsync();
            StopLoadingShows();
            if (reset)
            {
                Shows.Clear();
                Page = 0;
            }

            var watch = Stopwatch.StartNew();
            Page++;
            if (Page > 1 && Shows.Count == MaxNumberOfShows)
            {
                Page--;
                LoadingSemaphore.Release();
                return;
            }

            Logger.Info(
                $"Loading shows popular page {Page}...");
            HasLoadingFailed = false;
            try
            {
                IsLoadingShows = true;
                var result =
                    await ShowService.GetShowsAsync(Page,
                            MaxShowsPerPage,
                            Rating * 10,
                            "watching",
                            CancellationLoadingShows.Token,
                            Genre)
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
                    $"Error while loading shows popular page {Page}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded shows popular page {Page} in {elapsedMs} milliseconds.");
                LoadingSemaphore.Release();
            }
        }
    }
}