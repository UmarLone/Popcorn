using System;
using System.Collections.Generic;
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

        private string DownloadSubtitleToPath(string path, Subtitle subtitle, string newSubtitleName)
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
                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(subtitle.SubTitleDownloadLink, tempZipName);
                }

                UnZipSubtitleFileToFile(tempZipName, destinationfile);

            }
            finally
            {
                File.Delete(tempZipName);
            }

            return destinationfile;
        }

        public IEnumerable<Language> GetSubLanguages()
        {
            //get system language
            return GetSubLanguages("en");
        }

        private IEnumerable<Language> GetSubLanguages(string language)
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

        private void UnZipSubtitleFileToFile(string zipFileName, string subFileName)
        {
            using (FileStream subFile = File.OpenWrite(subFileName))
            using (FileStream tempFile = File.OpenRead(zipFileName))
            {
                var gzip = new GZipStream(tempFile, CompressionMode.Decompress);
                gzip.CopyTo(subFile);
            }
        }

        private Subtitle BuildSubtitleObject(SearchSubtitlesInfo info)
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

        private Language BuildLanguageObject(GetSubLanguagesInfo info)
        {
            var language = new Language
            {
                LanguageName = info.LanguageName,
                SubLanguageID = info.SubLanguageID,
                ISO639 = info.ISO639
            };
            return language;
        }

        private void VerifyResponseCode(ResponseBase response)
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
                throw new OSDBException($"Unexpected error response {response.status}");
            }
        }
    }
}