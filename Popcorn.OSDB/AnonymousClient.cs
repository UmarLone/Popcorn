using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net;
using CookComputing.XmlRpc;
using Popcorn.OSDB.Backend;

namespace Popcorn.OSDB
{
    public class AnonymousClient : IAnonymousClient
    {
        private bool _disposed;
        private readonly IOsdb _proxy;
        private string _token;

        internal AnonymousClient(IOsdb proxy)
        {
            _proxy = proxy;
        }

        internal void Login(string username, string password, string language, string userAgent)
        {
            var response = _proxy.Login(username, password, language, userAgent);
            VerifyResponseCode(response);
            _token = response.token;
        }

        public IList<Subtitle> SearchSubtitlesFromFile(string languages, string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var file = new FileInfo(filename);
            if (!file.Exists)
            {
                throw new ArgumentException("File doesn't exist", nameof(filename));
            }
            var request = new SearchSubtitlesRequest
            {
                sublanguageid = languages,
                moviehash = HashHelper.ToHexadecimal(HashHelper.ComputeMovieHash(filename)),
                moviebytesize = file.Length.ToString(),
                imdbid = string.Empty,
                query = string.Empty
            };


            return SearchSubtitlesInternal(request);
        }

        public IList<Subtitle> SearchSubtitlesFromImdb(string languages, string imdbId)
        {
            if (string.IsNullOrEmpty(imdbId))
            {
                throw new ArgumentNullException(nameof(imdbId));
            }
            var request = new SearchSubtitlesRequest
            {
                sublanguageid = languages,
                imdbid = imdbId
            };
            return SearchSubtitlesInternal(request);
        }

        public IList<Subtitle> SearchSubtitlesFromQuery(string languages, string query, int? season = null,
            int? episode = null)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }
            var request = new SearchSubtitlesRequest
            {
                sublanguageid = languages,
                query = query,
                season = season,
                episode = episode
            };
            return SearchSubtitlesInternal(request);
        }

        private IList<Subtitle> SearchSubtitlesInternal(SearchSubtitlesRequest request)
        {
            var response = _proxy.SearchSubtitles(_token, new[] {request});
            VerifyResponseCode(response);

            var subtitles = new List<Subtitle>();

            var subtitlesInfo = response.data as object[];
            if (null != subtitlesInfo)
            {
                foreach (var infoObject in subtitlesInfo)
                {
                    var subInfo = SimpleObjectMapper.MapToObject<SearchSubtitlesInfo>((XmlRpcStruct) infoObject);
                    subtitles.Add(BuildSubtitleObject(subInfo));
                }
            }
            return subtitles;
        }

        public string DownloadSubtitleToPath(string path, Subtitle subtitle)
        {
            return DownloadSubtitleToPath(path, subtitle, null);
        }

        public string DownloadSubtitleToPath(string path, Subtitle subtitle, string newSubtitleName)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (null == subtitle)
            {
                throw new ArgumentNullException(nameof(subtitle));
            }
            if (!Directory.Exists(path))
            {
                throw new ArgumentException("path should point to a valid location");
            }

            string destinationfile = Path.Combine(path,
                (string.IsNullOrEmpty(newSubtitleName)) ? subtitle.SubtitleFileName : newSubtitleName);
            string tempZipName = Path.GetTempFileName();
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(subtitle.SubTitleDownloadLink, tempZipName);

                UnZipSubtitleFileToFile(tempZipName, destinationfile);

            }
            finally
            {
                File.Delete(tempZipName);
            }

            return destinationfile;
        }

        public long CheckSubHash(string subHash)
        {
            var response = _proxy.CheckSubHash(_token, new[] {subHash});
            VerifyResponseCode(response);

            long idSubtitleFile = 0;
            var hashInfo = response.data as XmlRpcStruct;
            if (null != hashInfo && hashInfo.ContainsKey(subHash))
            {
                idSubtitleFile = Convert.ToInt64(hashInfo[subHash]);
            }

            return idSubtitleFile;
        }

        public IEnumerable<MovieInfo> CheckMovieHash(string moviehash)
        {
            var response = _proxy.CheckMovieHash(_token, new[] {moviehash});
            VerifyResponseCode(response);

            var movieInfoList = new List<MovieInfo>();

            var hashInfo = response.data as XmlRpcStruct;
            if (null != hashInfo && hashInfo.ContainsKey(moviehash))
            {
                var movieInfoArray = hashInfo[moviehash] as object[];
                if (movieInfoArray != null)
                    movieInfoList.AddRange(from XmlRpcStruct movieInfoStruct in movieInfoArray
                        select SimpleObjectMapper.MapToObject<CheckMovieHashInfo>(movieInfoStruct)
                        into movieInfo
                        select BuildMovieInfoObject(movieInfo));
            }

            return movieInfoList;
        }

        public IEnumerable<Language> GetSubLanguages()
        {
            //get system language
            return GetSubLanguages("en");
        }

        public IEnumerable<Language> GetSubLanguages(string language)
        {
            var response = _proxy.GetSubLanguages(language);
            VerifyResponseCode(response);

            IList<Language> languages = new List<Language>();
            foreach (var languageInfo in response.data)
            {
                languages.Add(BuildLanguageObject(languageInfo));
            }
            return languages;
        }

        public IEnumerable<Movie> SearchMoviesOnImdb(string query)
        {
            var response = _proxy.SearchMoviesOnIMDB(_token, query);
            VerifyResponseCode(response);

            IList<Movie> movies = new List<Movie>();

            if (response.data.Length == 1 && string.IsNullOrEmpty(response.data.First().id))
            {
                // no match found
                return movies;
            }

            foreach (var movieInfo in response.data)
            {
                movies.Add(BuildMovieObject(movieInfo));
            }
            return movies;
        }

        public MovieDetails GetImdbMovieDetails(string imdbId)
        {
            var response = _proxy.GetIMDBMovieDetails(_token, imdbId);
            VerifyResponseCode(response);

            var movieDetails = BuildMovieDetailsObject(response.data);
            return movieDetails;
        }

        public void NoOperation()
        {
            var response = _proxy.NoOperation(_token);
            VerifyResponseCode(response);
        }

        public IEnumerable<UserComment> GetComments(string idsubtitle)
        {
            var response = _proxy.GetComments(_token, new[] {idsubtitle});
            VerifyResponseCode(response);

            var comments = new List<UserComment>();
            var commentsStruct = response.data as XmlRpcStruct;
            if (commentsStruct == null)
                return comments;

            if (commentsStruct.ContainsKey("_" + idsubtitle))
            {
                object[] commentsList = commentsStruct["_" + idsubtitle] as object[];
                if (commentsList != null)
                {
                    foreach (XmlRpcStruct commentStruct in commentsList)
                    {
                        var comment = SimpleObjectMapper.MapToObject<CommentsData>(commentStruct);
                        comments.Add(BuildUserCommentObject(comment));
                    }
                }
            }

            return comments;
        }

        public string DetectLanguge(string data)
        {
            var bytes = GzipString(data);
            var text = Convert.ToBase64String(bytes);

            var response = _proxy.DetectLanguage(_token, new[] {text});
            VerifyResponseCode(response);

            var languagesStruct = response.data as XmlRpcStruct;
            if (languagesStruct == null)
                return null;

            foreach (string key in languagesStruct.Keys)
            {
                return languagesStruct[key].ToString();
            }
            return null;
        }

        public void ReportWrongMovieHash(string idSubMovieFile)
        {
            var response = _proxy.ReportWrongMovieHash(_token, idSubMovieFile);
            VerifyResponseCode(response);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && !string.IsNullOrEmpty(_token))
                {
                    try
                    {
                        _proxy.Logout(_token);
                    }
                    catch
                    {
                        //soak it. We don't want exception on disposing. It's better to let the session timeout.
                    }
                    _token = null;
                }
                _disposed = true;
            }
        }

        ~AnonymousClient()
        {
            Dispose(false);
        }

        private static byte[] GzipString(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        private static void UnZipSubtitleFileToFile(string zipFileName, string subFileName)
        {
            using (FileStream subFile = File.OpenWrite(subFileName))
            using (FileStream tempFile = File.OpenRead(zipFileName))
            {
                var gzip = new GZipStream(tempFile, CompressionMode.Decompress);
                gzip.CopyTo(subFile);
            }
        }

        private static Subtitle BuildSubtitleObject(SearchSubtitlesInfo info)
        {
            var sub = new Subtitle
            {
                SubtitleId = info.IDSubtitle,
                SubtitleHash = info.SubHash,
                SubtitleFileName = info.SubFileName,
                SubTitleDownloadLink = new Uri(info.SubDownloadLink),
                SubtitlePageLink = new Uri(info.SubtitlesLink),
                LanguageId = info.SubLanguageID,
                LanguageName = info.LanguageName,
                Rating = info.SubRating,
                Bad = info.SubBad,

                ImdbId = info.IDMovieImdb,
                MovieId = info.IDMovie,
                MovieName = info.MovieName,
                OriginalMovieName = info.MovieNameEng,
                MovieYear = int.Parse(info.MovieYear)
            };
            return sub;
        }

        private static MovieInfo BuildMovieInfoObject(CheckMovieHashInfo info)
        {
            var movieInfo = new MovieInfo
            {
                MovieHash = info.MovieHash,
                MovieImdbID = info.MovieImdbID,
                MovieYear = info.MovieYear,
                MovieName = info.MovieName,
                SeenCount = info.SeenCount
            };
            return movieInfo;
        }

        private static Language BuildLanguageObject(GetSubLanguagesInfo info)
        {
            var language = new Language
            {
                LanguageName = info.LanguageName,
                SubLanguageID = info.SubLanguageID,
                ISO639 = info.ISO639
            };
            return language;
        }

        private static Movie BuildMovieObject(MoviesOnIMDBInfo info)
        {
            var movie = new Movie
            {
                Id = Convert.ToInt64(info.id),
                Title = info.title
            };
            return movie;
        }

        private static MovieDetails BuildMovieDetailsObject(IMDBMovieDetails info)
        {
            var movie = new MovieDetails
            {
                Aka = info.aka,
                Cast = SimpleObjectMapper.MapToDictionary(info.cast as XmlRpcStruct),
                Cover = info.cover,
                Id = info.id,
                Rating = info.rating,
                Title = info.title,
                Votes = info.votes,
                Year = info.year,
                Country = info.country,
                Directors = SimpleObjectMapper.MapToDictionary(info.directors as XmlRpcStruct),
                Duration = info.duration,
                Genres = info.genres,
                Language = info.language,
                Tagline = info.tagline,
                Trivia = info.trivia,
                Writers = SimpleObjectMapper.MapToDictionary(info.writers as XmlRpcStruct)
            };
            return movie;
        }

        private static UserComment BuildUserCommentObject(CommentsData info)
        {
            var comment = new UserComment
            {
                Comment = info.Comment,
                Created = info.Created,
                IDSubtitle = info.IDSubtitle,
                UserID = info.UserID,
                UserNickName = info.UserNickName
            };
            return comment;
        }

        private static void VerifyResponseCode(ResponseBase response)
        {
            if (null == response)
            {
                throw new ArgumentNullException(nameof(response));
            }
            if (string.IsNullOrEmpty(response.status))
            {
                //aperantly there are some methods that dont define 'status'
                return;
            }

            int responseCode = int.Parse(response.status.Substring(0, 3));
            if (responseCode >= 400)
            {
                //TODO: Create Exception type
                throw new Exception($"Unexpected error response {response.status}");
            }
        }
    }
}