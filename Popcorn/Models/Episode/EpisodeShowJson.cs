using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using Popcorn.Models.Subtitles;
using Popcorn.Models.Torrent.Show;
using RestSharp.Deserializers;
using Popcorn.Models.Media;

namespace Popcorn.Models.Episode
{
    public class EpisodeShowJson : ObservableObject, IMediaFile
    {
        private bool _watchInFullHqQuality;

        private string _title;

        private string _filePath;

        private string _imdbId;

        private ObservableCollection<Subtitle> _availableSubtitles =
            new ObservableCollection<Subtitle>();

        private Subtitle _selectedSubtitle;

        private TorrentShowNodeJson _torrents;

        private long _firstAired;

        private bool _dateBased;

        private string _overview;

        private int _episodeNumber;

        private int _season;

        private int? _tvdbId;

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
            get => _availableSubtitles;
            set { Set(() => AvailableSubtitles, ref _availableSubtitles, value); }
        }

        /// <summary>
        /// Selected subtitle
        /// </summary>
        public Subtitle SelectedSubtitle
        {
            get => _selectedSubtitle;
            set { Set(() => SelectedSubtitle, ref _selectedSubtitle, value); }
        }

        [DeserializeAs(Name = "torrents")]
        public TorrentShowNodeJson Torrents
        {
            get => _torrents;
            set => Set(ref _torrents, value);
        }

        [DeserializeAs(Name = "first_aired")]
        public long FirstAired
        {
            get => _firstAired;
            set => Set(ref _firstAired, value);
        }

        [DeserializeAs(Name = "date_based")]
        public bool DateBased
        {
            get => _dateBased;
            set => Set(ref _dateBased, value);
        }

        [DeserializeAs(Name = "overview")]
        public string Overview
        {
            get => _overview;
            set => Set(ref _overview, value);
        }

        [DeserializeAs(Name = "title")]
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        [DeserializeAs(Name = "episode")]
        public int EpisodeNumber
        {
            get => _episodeNumber;
            set => Set(ref _episodeNumber, value);
        }

        [DeserializeAs(Name = "season")]
        public int Season
        {
            get => _season;
            set => Set(ref _season, value);
        }

        [DeserializeAs(Name = "tvdb_id")]
        public int? TvdbId
        {
            get => _tvdbId;
            set => Set(ref _tvdbId, value);
        }
    }
}