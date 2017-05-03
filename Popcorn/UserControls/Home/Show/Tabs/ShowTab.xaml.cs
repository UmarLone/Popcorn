using System.Windows.Controls;
using Popcorn.ViewModels.Pages.Home.Show.Tabs;

namespace Popcorn.UserControls.Home.Show.Tabs
{
    /// <summary>
    /// Logique d'interaction pour ShowTab.xaml
    /// </summary>
    public partial class ShowTab
    {
        public ShowTab()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load shows if control has reached bottom position
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private async void ScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var totalHeight = e.VerticalOffset + e.ViewportHeight;
            if (totalHeight < 2d / 3d * e.ExtentHeight) return;
            var vm = DataContext as ShowTabsViewModel;
            if (vm == null) return;
            if (vm is PopularShowTabViewModel || vm is GreatestShowTabViewModel || vm is RecentShowTabViewModel)
            {
                if (!vm.IsLoadingShows)
                    await vm.LoadShowsAsync().ConfigureAwait(false);
            }
            else if (vm is SearchShowTabViewModel)
            {
                var searchVm = vm as SearchShowTabViewModel;
                if (!searchVm.IsLoadingShows)
                    await searchVm.SearchShowsAsync(searchVm.SearchFilter).ConfigureAwait(false);
            }
        }
    }
}