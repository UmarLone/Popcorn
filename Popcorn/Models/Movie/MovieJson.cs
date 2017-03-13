using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Models.Cast;
using Popcorn.Models.Subtitles;
using Popcorn.Models.Torrent;
using RestSharp.Deserializers;

namespace Popcorn.Models.Movie
{
    public class MovieJson : ObservableObject
    {
        private List<CastJson> _cast;

        private ObservableCollection<Subtitle> _availableSubtitles =
            new ObservableCollection<Subtitle>();

        private int _imdbId;
        private string _dateUploaded;
        private string _posterImage;
        private string _backdropImage;
        private int _dateUploadedUnix;
        private string _descriptionFull;
        private string _descriptionIntro;
        private int _downloadCount;
        private string _filePath;
        private bool _fullHdAvailable;
        private List<string> _genres;
        private bool _hasBeenSeen;
        private string _backgroundImage;
        private string _largeCoverImage;
        private string _largeScreenshotImage1;
        private string _largeScreenshotImage2;
        private string _largeScreenshotImage3;
        private string _mediumCoverImage;
        private string _mediumScreenshotImage1;
        private string _mediumScreenshotImage2;
        private string _mediumScreenshotImage3;
        private string _smallCoverImage;
        private string _imdbCode;
        private bool _isFavorite;
        private string _language;
        private int _likeCount;
        private string _mpaRating;
        private double _rating;
        private int _runtime;
        private Subtitle _selectedSubtitle;
        private string _title;
        private string _titleLong;
        private string _slug;
        private List<TorrentJson> _torrents;
        private string _url;
        private bool _watchInFullHdQuality;
        private int _year;
        private string _ytTrailerCode;

        [DeserializeAs(Name = "url")]
        public string Url
        {
            get { return _url; }
            set { Set(() => Url, ref _url, value); }
        }

        [DeserializeAs(Name = "imdb_code")]
        public string ImdbCode
        {
            get { return _imdbCode; }
            set { Set(() => ImdbCode, ref _imdbCode, value); }
        }

        [DeserializeAs(Name = "title")]
        public string Title
        {
            get { return _title; }
            set { Set(() => Title, ref _title, value); }
        }

        [DeserializeAs(Name = "title_long")]
        public string TitleLong
        {
            get { return _titleLong; }
            set { Set(() => TitleLong, ref _titleLong, value); }
        }

        [DeserializeAs(Name = "slug")]
        public string Slug
        {
            get { return _slug; }
            set { Set(() => Slug, ref _slug, value); }
        }

        [DeserializeAs(Name = "year")]
        public int Year
        {
            get { return _year; }
            set { Set(() => Year, ref _year, value); }
        }

        [DeserializeAs(Name = "rating")]
        public double Rating
        {
            get { return _rating; }
            set { Set(() => Rating, ref _rating, value); }
        }

        [DeserializeAs(Name = "runtime")]
        public int Runtime
        {
            get { return _runtime; }
            set { Set(() => Runtime, ref _runtime, value); }
        }

        [DeserializeAs(Name = "genres")]
        public List<string> Genres
        {
            get { return _genres; }
            set { Set(() => Genres, ref _genres, value); }
        }

        [DeserializeAs(Name = "language")]
        public string Language
        {
            get { return _language; }
            set { Set(() => Language, ref _language, value); }
        }

        [DeserializeAs(Name = "mpa_rating")]
        public string MpaRating
        {
            get { return _mpaRating; }
            set { Set(() => MpaRating, ref _mpaRating, value); }
        }

        [DeserializeAs(Name = "download_count")]
        public int DownloadCount
        {
            get { return _downloadCount; }
            set { Set(() => DownloadCount, ref _downloadCount, value); }
        }

        [DeserializeAs(Name = "like_count")]
        public int LikeCount
        {
            get { return _likeCount; }
            set { Set(() => LikeCount, ref _likeCount, value); }
        }

        [DeserializeAs(Name = "description_intro")]
        public string DescriptionIntro
        {
            get { return _descriptionIntro; }
            set { Set(() => DescriptionIntro, ref _descriptionIntro, value); }
        }

        [DeserializeAs(Name = "description_full")]
        public string DescriptionFull
        {
            get { return _descriptionFull; }
            set { Set(() => DescriptionFull, ref _descriptionFull, value); }
        }

        [DeserializeAs(Name = "yt_trailer_code")]
        public string YtTrailerCode
        {
            get { return _ytTrailerCode; }
            set { Set(() => YtTrailerCode, ref _ytTrailerCode, value); }
        }

        [DeserializeAs(Name = "cast")]
        public List<CastJson> Cast
        {
            get { return _cast; }
            set { Set(() => Cast, ref _cast, value); }
        }

        [DeserializeAs(Name = "torrents")]
        public List<TorrentJson> Torrents
        {
            get { return _torrents; }
            set { Set(() => Torrents, ref _torrents, value); }
        }

        [DeserializeAs(Name = "date_uploaded")]
        public string DateUploaded
        {
            get { return _dateUploaded; }
            set { Set(() => DateUploaded, ref _dateUploaded, value); }
        }

        [DeserializeAs(Name = "poster_image")]
        public string PosterImage
        {
            get { return _posterImage; }
            set { Set(() => PosterImage, ref _posterImage, value); }
        }

        [DeserializeAs(Name = "backdrop_image")]
        public string BackdropImage
        {
            get { return _backdropImage; }
            set { Set(() => BackdropImage, ref _backdropImage, value); }
        }

        [DeserializeAs(Name = "date_uploaded_unix")]
        public int DateUploadedUnix
        {
            get { return _dateUploadedUnix; }
            set { Set(() => DateUploadedUnix, ref _dateUploadedUnix, value); }
        }

        [DeserializeAs(Name = "background_image")]
        public string BackgroundImage
        {
            get { return _backgroundImage; }
            set { Set(() => BackgroundImage, ref _backgroundImage, value); }
        }

        [DeserializeAs(Name = "small_cover_image")]
        public string SmallCoverImage
        {
            get { return _smallCoverImage; }
            set { Set(() => SmallCoverImage, ref _smallCoverImage, value); }
        }

        [DeserializeAs(Name = "medium_cover_image")]
        public string MediumCoverImage
        {
            get { return _mediumCoverImage; }
            set { Set(() => MediumCoverImage, ref _mediumCoverImage, value); }
        }

        [DeserializeAs(Name = "large_cover_image")]
        public string LargeCoverImage
        {
            get { return _largeCoverImage; }
            set { Set(() => LargeCoverImage, ref _largeCoverImage, value); }
        }

        [DeserializeAs(Name = "medium_screenshot_image1")]
        public string MediumScreenshotImage1
        {
            get { return _mediumScreenshotImage1; }
            set { Set(() => MediumScreenshotImage1, ref _mediumScreenshotImage1, value); }
        }

        [DeserializeAs(Name = "medium_screenshot_image2")]
        public string MediumScreenshotImage2
        {
            get { return _mediumScreenshotImage2; }
            set { Set(() => MediumScreenshotImage2, ref _mediumScreenshotImage2, value); }
        }

        [DeserializeAs(Name = "medium_screenshot_image3")]
        public string MediumScreenshotImage3
        {
            get { return _mediumScreenshotImage3; }
            set { Set(() => MediumScreenshotImage3, ref _mediumScreenshotImage3, value); }
        }

        [DeserializeAs(Name = "large_screenshot_image1")]
        public string LargeScreenshotImage1
        {
            get { return _largeScreenshotImage1; }
            set { Set(() => LargeScreenshotImage1, ref _largeScreenshotImage1, value); }
        }

        [DeserializeAs(Name = "large_screenshot_image2")]
        public string LargeScreenshotImage2
        {
            get { return _largeScreenshotImage2; }
            set { Set(() => LargeScreenshotImage2, ref _largeScreenshotImage2, value); }
        }

        [DeserializeAs(Name = "large_screenshot_image3")]
        public string LargeScreenshotImage3
        {
            get { return _largeScreenshotImage3; }
            set { Set(() => LargeScreenshotImage3, ref _largeScreenshotImage3, value); }
        }

        /// <summary>
        /// Local path of the downloaded movie file
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
            set { Set(() => FilePath, ref _filePath, value); }
        }

        /// <summary>
        /// Decide if movie has to be watched in full HQ or not
        /// </summary>
        public bool WatchInFullHdQuality
        {
            get { return _watchInFullHdQuality; }
            set
            {
                var odlValue = _watchInFullHdQuality;
                Set(() => WatchInFullHdQuality, ref _watchInFullHdQuality, value);
                Messenger.Default.Send(new PropertyChangedMessage<bool>(this,
                    odlValue, value, nameof(WatchInFullHdQuality)));
            }
        }

        /// <summary>
        /// Indicate if full HQ quality is available
        /// </summary>
        public bool FullHdAvailable
        {
            get { return _fullHdAvailable; }
            set { Set(() => FullHdAvailable, ref _fullHdAvailable, value); }
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

        /// <summary>
        /// Indicate if movie is favorite
        /// </summary>
        public bool IsFavorite
        {
            get { return _isFavorite; }
            set { Set(() => IsFavorite, ref _isFavorite, value); }
        }

        /// <summary>
        /// Indicate if movie has been seen by the user
        /// </summary>
        public bool HasBeenSeen
        {
            get { return _hasBeenSeen; }
            set { Set(() => HasBeenSeen, ref _hasBeenSeen, value); }
        }

        /// <summary>
        /// Imdb id
        /// </summary>
        public int ImdbId
        {
            get { return _imdbId; }
            set { Set(() => ImdbId, ref _imdbId, value); }
        }
    }
}