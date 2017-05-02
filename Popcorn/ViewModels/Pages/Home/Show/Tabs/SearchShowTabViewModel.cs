using System;
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
using Popcorn.Services.Shows.Show;

namespace Popcorn.ViewModels.Pages.Home.Show.Tabs
{
    public class SearchShowTabViewModel : ShowTabsViewModel
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the SearchMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="showService">Show service</param>
        public SearchShowTabViewModel(IApplicationService applicationService, IShowService showService)
            : base(applicationService, showService)
        {
            RegisterMessages();
            RegisterCommands();
            TabName = LocalizationProviderHelper.GetLocalizedValue<string>("SearchTitleTab");
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

            var watch = Stopwatch.StartNew();

            Page++;

            if (Page > 1 && Shows.Count == MaxNumberOfShows) return;

            Logger.Info(
                $"Loading page {Page} with criteria: {searchFilter}");

            HasLoadingFailed = false;

            try
            {
                SearchFilter = searchFilter;

                IsLoadingShows = true;

                var movies =
                    await ShowService.SearchShowsAsync(searchFilter,
                        Page,
                        MaxNumberOfShows,
                        Genre,
                        Rating,
                        CancellationLoadingShows.Token).ConfigureAwait(false);

                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var moviesList = movies.Item1.ToList();
                    Shows.AddRange(moviesList);
                    IsLoadingShows = false;
                    IsLoadingShows = Shows.Any();
                    CurrentNumberOfShows = Shows.Count;
                    MaxNumberOfShows = movies.Item2;
                });
            }
            catch (Exception exception)
            {
                Page--;
                Logger.Error(
                    $"Error while loading page {Page} with criteria {searchFilter}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded page {Page} with criteria {searchFilter} in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<ChangeLanguageMessage>(
                this,
                language => TabName = LocalizationProviderHelper.GetLocalizedValue<string>("SearchTitleTab"));

            Messenger.Default.Register<PropertyChangedMessage<GenreJson>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Genre) && Genre.Equals(e.NewValue)) return;
                StopLoadingShows();
                Page = 0;
                Shows.Clear();
                await SearchShowsAsync(SearchFilter);
            });

            Messenger.Default.Register<PropertyChangedMessage<double>>(this, async e =>
            {
                if (e.PropertyName != GetPropertyName(() => Rating) && Rating.Equals(e.NewValue)) return;
                StopLoadingShows();
                Page = 0;
                Shows.Clear();
                await SearchShowsAsync(SearchFilter);
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
                await SearchShowsAsync(SearchFilter);
            });
        }
    }
}