﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Messaging;
using Popcorn.Models.Localization;
using Popcorn.Models.Movie;
using Popcorn.Models.Shows;
using Popcorn.Models.User;
using Popcorn.Services.Movies.Movie;
using Popcorn.Utils;
using RestSharp;
using WPFLocalizeExtension.Engine;

namespace Popcorn.Services.User
{
    /// <summary>
    /// Services used to interact with user history
    /// </summary>
    public class UserService : IUserService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Services used to interact with movies
        /// </summary>
        private readonly IMovieService _movieService;

        private readonly string _userId;

        private UserJson User { get; set; }

        public UserService(IMovieService movieService, string userId)
        {
            _movieService = movieService;
            _userId = userId;
        }

        private async Task GetHistoryAsync()
        {
            var restClient = new RestClient(Utils.Constants.PopcornApi);
            var request = new RestRequest("/{segment}/{userId}", Method.GET);
            request.AddUrlSegment("segment", "user");
            request.AddUrlSegment("userId", _userId);
            var response = await restClient.ExecuteTaskAsync<UserJson>(request);
            if (response.ErrorException != null)
                throw response.ErrorException;
            User = response.Data;
        }

        private async Task UpdateHistoryAsync()
        {
            var restClient = new RestClient(Utils.Constants.PopcornApi);
            var request = new RestRequest("/{segment}", Method.POST);
            request.AddUrlSegment("segment", "user");
            request.AddJsonBody(User);
            var response = await restClient.ExecuteTaskAsync<UserJson>(request);
            if (response.ErrorException != null)
                throw response.ErrorException;
            User = response.Data;
        }

        /// <summary>
        /// Set if movies have been seen or set as favorite
        /// </summary>
        /// <param name="movies">All movies to compute</param>
        public async Task SyncMovieHistoryAsync(IEnumerable<MovieJson> movies)
        {
            if (movies == null) throw new ArgumentNullException(nameof(movies));
            var watch = Stopwatch.StartNew();

            try
            {
                await GetHistoryAsync();
                foreach (var movie in movies)
                {
                    var updatedMovie = User.MovieHistory.FirstOrDefault(p => p.ImdbId == movie.ImdbCode);
                    if (updatedMovie == null) continue;
                    movie.IsFavorite = updatedMovie.Favorite;
                    movie.HasBeenSeen = updatedMovie.Seen;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SyncMovieHistoryAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SyncMovieHistoryAsync in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Set if shows have been seen or set as favorite
        /// </summary>
        /// <param name="shows">All shows to compute</param>
        public async Task SyncShowHistoryAsync(IEnumerable<ShowJson> shows)
        {
            if (shows == null) throw new ArgumentNullException(nameof(shows));
            var watch = Stopwatch.StartNew();

            try
            {
                await GetHistoryAsync();
                foreach (var show in shows)
                {
                    var updatedShow = User.ShowHistory.FirstOrDefault(p => p.ImdbId == show.ImdbId);
                    if (updatedShow == null) continue;
                    show.IsFavorite = updatedShow.Favorite;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SyncShowHistoryAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SyncShowHistoryAsync in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Set the movie
        /// </summary>
        /// <param name="movie">Movie</param>
        public async Task SetMovieAsync(MovieJson movie)
        {
            if (movie == null) throw new ArgumentNullException(nameof(movie));
            var watch = Stopwatch.StartNew();

            try
            {
                var movieToUpdate = User.MovieHistory.FirstOrDefault(a => a.ImdbId == movie.ImdbCode);
                if (movieToUpdate == null)
                {
                    User.MovieHistory.Add(new MovieHistoryJson
                    {
                        ImdbId = movie.ImdbCode,
                        Favorite = movie.IsFavorite,
                        Seen = movie.HasBeenSeen
                    });
                }
                else
                {
                    movieToUpdate.Seen = movie.HasBeenSeen;
                    movieToUpdate.Favorite = movie.IsFavorite;
                }

                await UpdateHistoryAsync();
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SetFavoriteMovieAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SetFavoriteMovieAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Set the show
        /// </summary>
        /// <param name="show">Show</param>
        public async Task SetShowAsync(ShowJson show)
        {
            if (show == null) throw new ArgumentNullException(nameof(show));
            var watch = Stopwatch.StartNew();

            try
            {
                var showToUpdate = User.ShowHistory.FirstOrDefault(a => a.ImdbId == show.ImdbId);
                if (showToUpdate == null)
                {
                    User.ShowHistory.Add(new ShowHistoryJson
                    {
                        ImdbId = show.ImdbId,
                        Favorite = show.IsFavorite,
                    });
                }
                else
                {
                    showToUpdate.Favorite = show.IsFavorite;
                }

                await UpdateHistoryAsync();
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SetShowAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SetShowAsync ({show.ImdbId}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Get seen movies
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        public async Task<(IEnumerable<string> movies, IEnumerable<string> allMovies, int nbMovies)> GetSeenMovies(int page)
        {
            await GetHistoryAsync();
            var movies = User.MovieHistory.Where(a => a.Seen).Select(a => a.ImdbId).ToList();
            var skip = (page - 1) * Constants.MaxMoviesPerPage;
            if (movies.Count <= Constants.MaxMoviesPerPage)
            {
                skip = 0;
            }

            return (movies.Skip(skip).Take(Constants.MaxMoviesPerPage), movies, movies.Count);
        }

        /// <summary>
        /// Get seen shows
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        public async Task<(IEnumerable<string> shows, IEnumerable<string> allShows, int nbShows)> GetSeenShows(int page)
        {
            await GetHistoryAsync();
            var shows = User.ShowHistory.Where(a => a.Seen).Select(a => a.ImdbId).ToList();
            var skip = (page - 1) * Constants.MaxShowsPerPage;
            if (shows.Count <= Constants.MaxShowsPerPage)
            {
                skip = 0;
            }

            return (shows.Skip(skip).Take(Constants.MaxShowsPerPage), shows, shows.Count);
        }

        /// <summary>
        /// Get favorites movies
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        public async Task<(IEnumerable<string> movies, IEnumerable<string> allMovies, int nbMovies)> GetFavoritesMovies(int page)
        {
            await GetHistoryAsync();
            var movies = User.MovieHistory.Where(a => a.Favorite).Select(a => a.ImdbId).ToList();
            var skip = (page - 1) * Constants.MaxMoviesPerPage;
            if (movies.Count <= Constants.MaxMoviesPerPage)
            {
                skip = 0;
            }

            return (movies.Skip(skip).Take(Constants.MaxMoviesPerPage), movies, movies.Count);
        }

        /// <summary>
        /// Get favorites shows
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        public async Task<(IEnumerable<string> shows, IEnumerable<string> allShows, int nbShows)> GetFavoritesShows(int page)
        {
            await GetHistoryAsync();
            var shows = User.ShowHistory.Where(a => a.Favorite).Select(a => a.ImdbId).ToList();
            var skip = (page - 1) * Constants.MaxShowsPerPage;
            if (shows.Count <= Constants.MaxShowsPerPage)
            {
                skip = 0;
            }

            return (shows.Skip(skip).Take(Constants.MaxShowsPerPage), shows, shows.Count);
        }

        /// <summary>
        /// Get the download rate
        /// </summary>
        /// <returns>Download rate</returns>
        public async Task<int> GetDownloadLimit()
        {
            await GetHistoryAsync();
            return User.DownloadLimit;
        }

        /// <summary>
        /// Get the upload rate
        /// </summary>
        /// <returns>Upload rate</returns>
        public async Task<int> GetUploadLimit()
        {
            await GetHistoryAsync();
            return User.UploadLimit;
        }

        /// <summary>
        /// Set the download rate
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task SetDownloadLimit(int limit)
        {
            await GetHistoryAsync();
            User.DownloadLimit = limit;
            await UpdateHistoryAsync();
        }

        /// <summary>
        /// Set the upload rate
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task SetUploadLimit(int limit)
        {
            await GetHistoryAsync();
            User.UploadLimit = limit;
            await UpdateHistoryAsync();
        }

        /// <summary>
        /// Get all available languages from the database
        /// </summary>
        /// <returns>All available languages</returns>
        public ICollection<LanguageJson> GetAvailableLanguages()
        {
            var watch = Stopwatch.StartNew();

            ICollection<LanguageJson> availableLanguages = new List<LanguageJson>();
            availableLanguages.Add(new EnglishLanguage());
            availableLanguages.Add(new FrenchLanguage());
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Logger.Debug(
                $"GetAvailableLanguages in {elapsedMs} milliseconds.");

            return availableLanguages;
        }

        /// <summary>
        /// Get the current language of the application
        /// </summary>
        /// <returns>Current language</returns>
        public async Task<LanguageJson> GetCurrentLanguageAsync()
        {
            LanguageJson currentLanguage = null;

            var watch = Stopwatch.StartNew();
            await GetHistoryAsync();
            var language = User.Language;
            if (language != null)
            {
                switch (language.Culture)
                {
                    case "en":
                        currentLanguage = new EnglishLanguage();
                        break;
                    case "fr":
                        currentLanguage = new FrenchLanguage();
                        break;
                    default:
                        currentLanguage = new EnglishLanguage();
                        break;
                }
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Logger.Debug(
                $"GetCurrentLanguageAsync in {elapsedMs} milliseconds.");

            return currentLanguage;
        }

        /// <summary>
        /// Set the current language of the application
        /// </summary>
        /// <param name="language">Language</param>
        public async Task SetCurrentLanguageAsync(LanguageJson language)
        {
            var watch = Stopwatch.StartNew();
            await GetHistoryAsync();
            User.Language.Culture = language.Culture;
            await UpdateHistoryAsync();

            ChangeLanguage(User.Language);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Logger.Debug(
                $"SetCurrentLanguageAsync ({User.Language.Name}) in {elapsedMs} milliseconds.");
        }

        /// <summary>
        /// Change language
        /// </summary>
        /// <param name="language"></param>
        private void ChangeLanguage(LanguageJson language)
        {
            if (language == null) throw new ArgumentNullException(nameof(language));
            _movieService.ChangeTmdbLanguage(language);
            LocalizeDictionary.Instance.Culture = new CultureInfo(language.Culture);
            Messenger.Default.Send(new ChangeLanguageMessage());
        }
    }
}