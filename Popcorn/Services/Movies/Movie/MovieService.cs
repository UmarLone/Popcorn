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
            TmdbClient = new TMDbClient(Constants.TmDbClientId)
            {
                MaxRetryCount = 10
            };
            TmdbClient.GetConfig();
        }

        /// <summary>
        /// TMDb client
        /// </summary>
        private TMDbClient TmdbClient { get; }

        /// <summary>
        /// Change the culture of TMDb
        /// </summary>
        /// <param name="language">Language to set</param>
        public void ChangeTmdbLanguage(ILanguage language) => TmdbClient.DefaultLanguage = language.Culture;

        /// <summary>
        /// Tmdb genres cached
        /// </summary>
        private List<Genre> _cachedGenres;


        /// <summary>
        /// Get movie by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Movie's Imdb code</param>
        /// <returns>The movie</returns>
        public async Task<MovieJson> GetMovieAsync(string imdbCode)
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
                    _cachedGenres = englishGenre;
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
                throw;
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
        /// <param name="movieId">Movie Id</param>
        /// <returns>Movies</returns>
        public async Task<List<MovieJson>> GetMoviesSimilarAsync(string imdbCode)
        {
            var watch = Stopwatch.StartNew();

            var movies = new List<MovieJson>();

            try
            {
                var movie = await TmdbClient.GetMovieAsync(imdbCode, MovieMethods.AlternativeTitles);
                var search = await TmdbClient.GetMovieSimilarAsync(movie.Id);
                if (search.TotalResults != 0)
                {
                    await search.Results.Select(a => a.Id).ParallelForEachAsync(async id =>
                    {
                        var res = await TmdbClient.GetMovieAsync(id);
                        if (res != null && !string.IsNullOrEmpty(res.ImdbId))
                        {
                            var movieToAdd = await GetMovieAsync(res.ImdbId);
                            if (movieToAdd != null)
                            {
                                movies.Add(movieToAdd);
                            }
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
        public async Task<Tuple<IEnumerable<MovieJson>, int>> GetPopularMoviesAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new MovieResponse();

            if (limit < 1 || limit > 50)
                limit = 20;

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

            var movies = wrapper.Movies ?? new List<MovieJson>();
            Parallel.ForEach(movies, async movie =>
            {
                await TranslateMovieAsync(movie);
            });

            var nbMovies = wrapper.TotalMovies;

            return new Tuple<IEnumerable<MovieJson>, int>(movies, nbMovies);
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
        public async Task<Tuple<IEnumerable<MovieJson>, int>> GetGreatestMoviesAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new MovieResponse();

            if (limit < 1 || limit > 50)
                limit = 20;

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

            var movies = wrapper.Movies ?? new List<MovieJson>();
            Parallel.ForEach(movies, async movie =>
            {
                await TranslateMovieAsync(movie);
            });

            var nbMovies = wrapper.TotalMovies;

            return new Tuple<IEnumerable<MovieJson>, int>(movies, nbMovies);
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
        public async Task<Tuple<IEnumerable<MovieJson>, int>> GetRecentMoviesAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new MovieResponse();

            if (limit < 1 || limit > 50)
                limit = 20;

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

            var movies = wrapper.Movies ?? new List<MovieJson>();
            Parallel.ForEach(movies, async movie =>
            {
                await TranslateMovieAsync(movie);
            });

            var nbMovies = wrapper.TotalMovies;

            return new Tuple<IEnumerable<MovieJson>, int>(movies, nbMovies);
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
        public async Task<Tuple<IEnumerable<MovieJson>, int>> SearchMoviesAsync(string criteria,
            int page,
            int limit,
            GenreJson genre,
            double ratingFilter,
            CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new MovieResponse();

            if (limit < 1 || limit > 50)
                limit = 20;

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

            var movies = wrapper.Movies ?? new List<MovieJson>();
            Parallel.ForEach(movies, async movie =>
            {
                await TranslateMovieAsync(movie);
            });

            var nbMovies = wrapper.TotalMovies;

            return new Tuple<IEnumerable<MovieJson>, int>(movies, nbMovies);
        }

        /// <summary>
        /// Translate movie informations (title, description, ...)
        /// </summary>
        /// <param name="movieToTranslate">Movie to translate</param>
        /// <param name="ct">Used to cancel translation</param>
        /// <returns>Task</returns>
        public async Task TranslateMovieAsync(MovieJson movieToTranslate)
        {
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
    }
}