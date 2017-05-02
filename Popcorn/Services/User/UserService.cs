using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Messaging;
using Popcorn.Models.Episode;
using Popcorn.Models.Localization;
using Popcorn.Models.Movie;
using Popcorn.Models.User;
using Popcorn.Services.Movies.Movie;
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
        public async Task SyncShowHistoryAsync(IEnumerable<EpisodeShowJson> shows)
        {
            if (shows == null) throw new ArgumentNullException(nameof(shows));
            var watch = Stopwatch.StartNew();

            try
            {
                await GetHistoryAsync();
                foreach (var show in shows)
                {
                    var updatedShow = User.MovieHistory.FirstOrDefault(p => p.ImdbId == show.ImdbId);
                    if (updatedShow == null) continue;
                    show.IsFavorite = updatedShow.Favorite;
                    show.HasBeenSeen = updatedShow.Seen;
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
        public async Task SetShowAsync(EpisodeShowJson show)
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
                        Seen = show.HasBeenSeen
                    });
                }
                else
                {
                    showToUpdate.Seen = show.HasBeenSeen;
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
        /// <returns>List of ImdbId</returns>
        public async Task<IEnumerable<string>> GetSeenMovies()
        {
            await GetHistoryAsync();
            return User.MovieHistory.Where(a => a.Seen).Select(a => a.ImdbId);
        }

        /// <summary>
        /// Get seen shows
        /// </summary>
        /// <returns>List of ImdbId</returns>
        public async Task<IEnumerable<string>> GetSeenShows()
        {
            await GetHistoryAsync();
            return User.ShowHistory.Where(a => a.Seen).Select(a => a.ImdbId);
        }

        /// <summary>
        /// Get favorites movies
        /// </summary>
        /// <returns>List of ImdbId</returns>
        public async Task<IEnumerable<string>> GetFavoritesMovies()
        {
            await GetHistoryAsync();
            return User.MovieHistory.Where(a => a.Favorite).Select(a => a.ImdbId);
        }

        /// <summary>
        /// Get favorites shows
        /// </summary>
        /// <returns>List of ImdbId</returns>
        public async Task<IEnumerable<string>> GetFavoritesShows()
        {
            await GetHistoryAsync();
            return User.ShowHistory.Where(a => a.Favorite).Select(a => a.ImdbId);
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