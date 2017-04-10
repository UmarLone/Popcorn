using System.Windows;

namespace Popcorn.UserControls.Home.Movie
{
    /// <summary>
    /// Logique d'interaction pour MovieUserControl.xaml
    /// </summary>
    public partial class MovieUserControl
    {
        public MovieUserControl()
        {
            InitializeComponent();
        }

        private void HamburgerButtonOnClick(object sender, RoutedEventArgs e)
        {
            SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
        }
    }
}