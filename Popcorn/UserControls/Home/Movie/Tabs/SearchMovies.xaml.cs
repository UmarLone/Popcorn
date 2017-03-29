using System.Windows.Controls;
using Popcorn.ViewModels.Pages.Home.Movie.Tabs;

namespace Popcorn.UserControls.Home.Movie.Tabs
{
    /// <summary>
    /// Interaction logic for SearchMovies.xaml
    /// </summary>
    public partial class SearchMovies
    {
        /// <summary>
        /// Initializes a new instance of the SearchMovies class.
        /// </summary>
        public SearchMovies()
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
            var vm = DataContext as SearchMovieTabViewModel;
            if (vm != null && !vm.IsLoadingMovies)
                await vm.SearchMoviesAsync(vm.SearchFilter);
        }
    }
}