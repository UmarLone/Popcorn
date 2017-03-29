using System.Windows.Controls;
using Popcorn.ViewModels.Pages.Home.Show.Tabs;

namespace Popcorn.UserControls.Home.Show.Tabs
{
    /// <summary>
    /// Logique d'interaction pour PopularShows.xaml
    /// </summary>
    public partial class PopularShows
    {
        public PopularShows()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load movies if control has reached bottom position
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private async void ScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var totalHeight = e.VerticalOffset + e.ViewportHeight;
            if (totalHeight < 2d / 3d * e.ExtentHeight) return;
            var vm = DataContext as PopularShowTabViewModel;
            if (vm != null && !vm.IsLoadingShows)
                await vm.LoadShowsAsync();
        }
    }
}
