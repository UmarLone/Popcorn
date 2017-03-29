using System;
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
using Popcorn.Services.Shows.Show;

namespace Popcorn.ViewModels.Pages.Home.Show.Tabs
{
    public class PopularShowTabViewModel : ShowTabsViewModel
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the PopularShowTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="showService">Show service</param>
        public PopularShowTabViewModel(IApplicationService applicationService, IShowService showService)
            : base(applicationService, showService)
        {
            RegisterMessages();
            RegisterCommands();
            TabName = LocalizationProviderHelper.GetLocalizedValue<string>("PopularShowTitleTab");
        }

        /// <summary>
        /// Load shows asynchronously
        /// </summary>
        public override async Task LoadShowsAsync()
        {
            var watch = Stopwatch.StartNew();

            Page++;

            if (Page > 1 && Shows.Count == MaxNumberOfShows) return;

            Logger.Info(
                $"Loading page {Page}...");

            HasLoadingFailed = false;

            try
            {
                IsLoadingShows = true;

                var shows =
                    await ShowService.GetPopularShowsAsync(Page,
                        MaxShowsPerPage,
                        Rating,
                        CancellationLoadingShows.Token,
                        Genre).ConfigureAwait(false);

                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Shows.AddRange(shows.Item1);
                    IsLoadingShows = false;
                    IsShowFound = Shows.Any();
                    CurrentNumberOfShows = Shows.Count;
                    MaxNumberOfShows = shows.Item2;
                });
            }
            catch (Exception exception)
            {
                Page--;
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
                    $"Loaded page {Page} in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {

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