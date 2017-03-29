using System.Windows.Controls;
using Popcorn.ViewModels.Pages.Home.Movie.Tabs;

namespace Popcorn.UserControls.Home.Movie.Tabs
{
    /// <summary>
    /// Interaction logic for RecentMovies.xaml
    /// </summary>
    public partial class RecentMovies
    {
        /// <summary>
        /// Initializes a new instance of the RecentMovies class.
        /// </summary>
        public RecentMovies()
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
            var vm = DataContext as RecentMovieTabViewModel;
            if (vm != null && !vm.IsLoadingMovies)
                await vm.LoadMoviesAsync();
        }
    }
}