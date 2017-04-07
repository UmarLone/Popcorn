using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Popcorn.Helpers;
using Popcorn.Models.Genre;
using Popcorn.Models.Localization;
using Popcorn.Models.Movie;
using RestSharp;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using System.Collections.Async;
using Popcorn.Models.Trailer;

namespace Popcorn.Services.Movies.Movie
{
    /// <summary>
    /// Services used to interact with movies
    /// </summary>
    public class MovieService : IMovieService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initialize a new instance of MovieService class
        /// </summary>
        public MovieService()
        {
            TmdbClient = new TMDbClient(Constants.TmDbClientId, true)
            {
                MaxRetryCount = 50
            };

            try
            {
                TmdbClient.GetConfig();
            }
            catch (Exception)
            {
                //TODO
            }
        }

        /// <summary>
        /// TMDb client
        /// </summary>
        private TMDbClient TmdbClient { get; }

        /// <summary>
        /// True if movie languages must be refreshed
        /// </summary>
        private bool _mustRefreshLanguage;

        /// <summary>
        /// Change the culture of TMDb
        /// </summary>
        /// <param name="language">Language to set</param>
        public void ChangeTmdbLanguage(ILanguage language)
        {
            if (TmdbClient.DefaultLanguage == null && language.Culture == "en")
            {
                _mustRefreshLanguage = false;
            }
            else if (TmdbClient.DefaultLanguage != language.Culture)
            {
                _mustRefreshLanguage = true;
            }
            else
            {
                _mustRefreshLanguage = false;
            }

            TmdbClient.DefaultLanguage = language.Culture;
        }

        /// <summary>
        /// Get movie by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Movie's Imdb code</param>
        /// <returns>The movie</returns>
        private async Task<MovieJson> GetMovieAsync(string imdbCode)
        {
            var watch = Stopwatch.StartNew();

            var restClient = new RestClient(Constants.PopcornApi);
            var request = new RestRequest("/{segment}/{movie}", Method.GET);
            request.AddUrlSegment("segment", "movies");
            request.AddUrlSegment("movie", imdbCode);
            MovieJson movie = new MovieJson();

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<MovieJson>(request);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                movie = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetPopularMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetPopularMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetMovieAsync ({imdbCode}) in {elapsedMs} milliseconds.");
            }

            return movie;
        }

        /// <summary>
        /// Get all movie's genres
        /// </summary>
        /// <param name="ct">Used to cancel loading genres</param>
        /// <returns>Genres</returns>
        public async Task<List<GenreJson>> GetGenresAsync(CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var genres = new List<GenreJson>();

            try
            {
                await Task.Run(async () =>
                {
                    var englishGenre = await TmdbClient.GetMovieGenresAsync(new EnglishLanguage().Culture);
                    genres.AddRange((await TmdbClient.GetMovieGenresAsync()).Select(genre => new GenreJson
                    {
                        EnglishName = englishGenre.FirstOrDefault(p => p.Id == genre.Id)?.Name,
                        TmdbGenre = genre
                    }));
                }, ct);
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetGenresAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetGenresAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetGenresAsync in {elapsedMs} milliseconds.");
            }

            return genres;
        }

        /// <summary>
        /// Get movies similar async
        /// </summary>
        /// <param name="movie">Movie</param>
        /// <returns>Movies</returns>
        public async Task<List<MovieJson>> GetMoviesSimilarAsync(MovieJson movie)
        {
            var watch = Stopwatch.StartNew();

            var movies = new List<MovieJson>();

            try
            {
                if (movie.Similars != null && movie.Similars.Any())
                {
                    await movie.Similars.ParallelForEachAsync(async imdbCode =>
                    {
                        var similar = await GetMovieAsync(imdbCode);
                        if (similar != null)
                        {
                            movies.Add(similar);
                        }
                    });
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetMoviesSimilarAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetMoviesSimilarAsync in {elapsedMs} milliseconds.");
            }

            return movies;
        }

        /// <summary>
        /// Get popular movies by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular movies and the number of movies found</returns>
        public async Task<(IEnumerable<MovieJson> movies, int nbMovies)> GetPopularMoviesAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new MovieResponse();

            if (limit < 1 || limit > 50)
                limit = Constants.MaxMoviesPerPage;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Constants.PopcornApi);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "movies");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("sort_by", "like_count");

            IRestResponse<MovieResponse> response;
            try
            {
                response = await restClient.ExecuteGetTaskAsync<MovieResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetPopularMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetPopularMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetPopularMoviesAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var result = wrapper?.Movies ?? new List<MovieJson>();
            Parallel.ForEach(result, async movie =>
            {
                await TranslateMovieAsync(movie);
            });

            var nbResult = wrapper?.TotalMovies ?? 0;

            return (result, nbResult);
        }

        /// <summary>
        /// Get greatest movies by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Top rated movies and the number of movies found</returns>
        public async Task<(IEnumerable<MovieJson> movies, int nbMovies)> GetGreatestMoviesAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new MovieResponse();

            if (limit < 1 || limit > 50)
                limit = Constants.MaxMoviesPerPage;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Constants.PopcornApi);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "movies");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("sort_by", "download_count");

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<MovieResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetGreatestMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetGreatestMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetGreatestMoviesAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var result = wrapper?.Movies ?? new List<MovieJson>();
            Parallel.ForEach(result, async movie =>
            {
                await TranslateMovieAsync(movie);
            });

            var nbResult = wrapper?.TotalMovies ?? 0;

            return (result, nbResult);
        }

        /// <summary>
        /// Get recent movies by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Recent movies and the number of movies found</returns>
        public async Task<(IEnumerable<MovieJson> movies, int nbMovies)> GetRecentMoviesAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new MovieResponse();

            if (limit < 1 || limit > 50)
                limit = Constants.MaxMoviesPerPage;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Constants.PopcornApi);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "movies");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("sort_by", "seeds");

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<MovieResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetRecentMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetRecentMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetRecentMoviesAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var result = wrapper?.Movies ?? new List<MovieJson>();
            Parallel.ForEach(result, async movie =>
            {
                await TranslateMovieAsync(movie);
            });

            var nbResult = wrapper?.TotalMovies ?? 0;

            return (result, nbResult);
        }

        /// <summary>
        /// Search movies by criteria
        /// </summary>
        /// <param name="criteria">Criteria used for search</param>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Searched movies and the number of movies found</returns>
        public async Task<(IEnumerable<MovieJson> movies, int nbMovies)> SearchMoviesAsync(string criteria,
            int page,
            int limit,
            GenreJson genre,
            double ratingFilter,
            CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new MovieResponse();

            if (limit < 1 || limit > 50)
                limit = Constants.MaxMoviesPerPage;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Constants.PopcornApi);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "movies");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("query_term", criteria);

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<MovieResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "SearchMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SearchMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SearchMoviesAsync ({criteria}, {page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var result = wrapper?.Movies ?? new List<MovieJson>();
            Parallel.ForEach(result, async movie =>
            {
                await TranslateMovieAsync(movie);
            });

            var nbResult = wrapper?.TotalMovies ?? 0;

            return (result, nbResult);
        }

        /// <summary>
        /// Translate movie informations (title, description, ...)
        /// </summary>
        /// <param name="movieToTranslate">Movie to translate</param>
        /// <returns>Task</returns>
        public async Task TranslateMovieAsync(MovieJson movieToTranslate)
        {
            if (!_mustRefreshLanguage) return;

            var watch = Stopwatch.StartNew();

            try
            {
                var movie = await TmdbClient.GetMovieAsync(movieToTranslate.ImdbCode,
                    MovieMethods.Credits);
                movieToTranslate.Title = movie?.Title;
                movieToTranslate.Genres = movie?.Genres?.Select(a => a.Name).ToList();
                movieToTranslate.DescriptionFull = movie?.Overview;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "TranslateMovieAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"TranslateMovieAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"TranslateMovieAsync ({movieToTranslate.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Get the link to the youtube trailer of a movie
        /// </summary>
        /// <param name="movie">The movie</param>
        /// <param name="ct">Used to cancel loading trailer</param>
        /// <returns>Video trailer</returns>
        public async Task<ResultContainer<Video>> GetMovieTrailerAsync(MovieJson movie, CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var trailers = new ResultContainer<Video>();
            try
            {
                await Task.Run(
                    async () => trailers = (await TmdbClient.GetMovieAsync(movie.ImdbCode, MovieMethods.Videos))?.Videos,
                    ct);
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetMovieTrailerAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetMovieTrailerAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetMovieTrailerAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }

            return trailers;
        }

        /// <summary>
        /// Get the video url of the trailer by its Youtube key
        /// </summary>
        /// <param name="key">Youtube trailer key</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Trailer url</returns>
        public async Task<string> GetVideoTrailerUrlAsync(string key, CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new TrailerResponse();

            var restClient = new RestClient(Constants.PopcornApi);
            var request = new RestRequest("/{segment}/{key}", Method.GET);
            request.AddUrlSegment("segment", "trailer");
            request.AddUrlSegment("key", key);

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<TrailerResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetVideoTrailerUrlAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetVideoTrailerUrlAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetVideoTrailerUrlAsync ({key}) in {elapsedMs} milliseconds.");
            }

            return wrapper.TrailerUrl ?? string.Empty;;
        }
    }
}