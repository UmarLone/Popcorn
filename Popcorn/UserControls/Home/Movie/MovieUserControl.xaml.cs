using System.Windows;
using System.Windows.Input;

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

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SplitView.IsPaneOpen = true;
        }
    }
}