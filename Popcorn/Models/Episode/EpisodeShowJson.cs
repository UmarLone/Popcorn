using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using Popcorn.Models.Subtitles;
using Popcorn.Models.Torrent.Show;
using RestSharp.Deserializers;

namespace Popcorn.Models.Episode
{
    public class EpisodeShowJson : ObservableObject
    {
        private bool _watchInFullHqQuality;

        private string _filePath;

        private string _imdbId;

        private ObservableCollection<Subtitle> _availableSubtitles =
            new ObservableCollection<Subtitle>();

        private Subtitle _selectedSubtitle;

        public bool WatchInFullHdQuality
        {
            get => _watchInFullHqQuality;
            set => Set(ref _watchInFullHqQuality, value);
        }

        public string FilePath
        {
            get => _filePath;
            set => Set(ref _filePath, value);
        }

        public string ImdbId
        {
            get => _imdbId;
            set => Set(ref _imdbId, value);
        }

        /// <summary>
        /// Available subtitles
        /// </summary>
        public ObservableCollection<Subtitle> AvailableSubtitles
        {
            get { return _availableSubtitles; }
            set { Set(() => AvailableSubtitles, ref _availableSubtitles, value); }
        }

        /// <summary>
        /// Selected subtitle
        /// </summary>
        public Subtitle SelectedSubtitle
        {
            get { return _selectedSubtitle; }
            set { Set(() => SelectedSubtitle, ref _selectedSubtitle, value); }
        }

        [DeserializeAs(Name = "torrents")]
        public TorrentShowNodeJson Torrents { get; set; }

        [DeserializeAs(Name = "first_aired")]
        public long FirstAired { get; set; }

        [DeserializeAs(Name = "date_based")]
        public bool DateBased { get; set; }

        [DeserializeAs(Name = "overview")]
        public string Overview { get; set; }

        [DeserializeAs(Name = "title")]
        public string Title { get; set; }

        [DeserializeAs(Name = "episode")]
        public int EpisodeNumber { get; set; }

        [DeserializeAs(Name = "season")]
        public int Season { get; set; }

        [DeserializeAs(Name = "tvdb_id")]
        public int? TvdbId { get; set; }
    }
}
