using System.Collections.Async;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using Popcorn.Models.ApplicationState;
using Popcorn.Services.Shows.Show;
using Popcorn.ViewModels.Pages.Home.Show.Tabs;

namespace Popcorn.ViewModels.Pages.Home.Show
{
    public class ShowPageViewModel : ObservableObject, IPageViewModel
    {
        /// <summary>
        /// <see cref="Caption"/>
        /// </summary>
        private string _caption;

        /// <summary>
        /// The selected tab
        /// </summary>
        private ShowTabsViewModel _selectedTab;

        /// <summary>
        /// <see cref="SelectedShowsIndexMenuTab"/>
        /// </summary>
        private int _selectedShowsIndexMenuTab;

        /// <summary>
        /// The tabs
        /// </summary>
        private ObservableCollection<ShowTabsViewModel> _tabs = new ObservableCollection<ShowTabsViewModel>();

        public ShowPageViewModel(IApplicationService applicationService, IShowService showService)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                Tabs.Add(new PopularShowTabViewModel(applicationService, showService));
                SelectedTab = Tabs.First();
                SelectedShowsIndexMenuTab = 0;
                var loadMoviesTask = Tabs.ParallelForEachAsync(async tab =>
                {
                    await tab.LoadShowsAsync();
                });

                await Task.WhenAll(new List<Task>
                {
                    loadMoviesTask
                });
            });
        }


        /// <summary>
        /// Selected index for movies menu
        /// </summary>
        public int SelectedShowsIndexMenuTab
        {
            get { return _selectedShowsIndexMenuTab; }
            set { Set(() => SelectedShowsIndexMenuTab, ref _selectedShowsIndexMenuTab, value); }
        }

        /// <summary>
        /// The selected tab
        /// </summary>
        public ShowTabsViewModel SelectedTab
        {
            get { return _selectedTab; }
            set { Set(() => SelectedTab, ref _selectedTab, value); }
        }

        /// <summary>
        /// Tabs shown into the interface
        /// </summary>
        public ObservableCollection<ShowTabsViewModel> Tabs
        {
            get { return _tabs; }
            set { Set(() => Tabs, ref _tabs, value); }
        }

        /// <summary>
        /// Tab caption 
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            set { Set(ref _caption, value); }
        }
    }
}
