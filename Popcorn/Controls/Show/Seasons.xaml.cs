using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Popcorn.Events;
using Popcorn.Models.Episode;
using Popcorn.Models.Shows;

namespace Popcorn.Controls.Show
{
    /// <summary>
    /// Logique d'interaction pour Seasons.xaml
    /// </summary>
    public partial class Seasons : UserControl
    {
        /// <summary>
        /// Current number property
        /// </summary>
        public static readonly DependencyProperty ShowProperty =
            DependencyProperty.Register("Show",
                typeof(ShowJson), typeof(Seasons),
                new PropertyMetadata(null, PropertyChangedCallback));

        /// <summary>
        /// The current number of shows
        /// </summary>
        public ShowJson Show
        {
            get => (ShowJson)GetValue(ShowProperty);
            set => SetValue(ShowProperty, value);
        }

        /// <summary>
        /// Raised when the selected season has changed
        /// </summary>
        public event EventHandler<SelectedSeasonChangedEventArgs> SelectedSeasonChanged;

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var seasons = dependencyObject as Seasons;
            var show = seasons.Show;
            var collection = new ObservableCollection<Season>();
            var episodesBySeason =
                seasons.Show.Episodes.GroupBy(r => r.Season)
                    .ToDictionary(t => t.Key, t => t.Select(r => r).ToList());
            foreach (var nbSeason in episodesBySeason.Keys.OrderBy(a => a))
            {
                collection.Add(new Season
                {
                    Label = $"Season {nbSeason}",
                    Number = nbSeason
                });
            }

            seasons.ComboSeasons.ItemsSource = collection;
            seasons.ComboSeasons.SelectedIndex = 0;
        }

        public Seasons()
        {
            InitializeComponent();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSeason = ComboSeasons.SelectedValue as Season;
            if (selectedSeason == null) return;
            SelectedSeasonChanged?.Invoke(this, new SelectedSeasonChangedEventArgs(selectedSeason.Number));
        }
    }

    public class Season
    {
        public int Number { get; set; }
        public string Label { get; set; }
    }
}
