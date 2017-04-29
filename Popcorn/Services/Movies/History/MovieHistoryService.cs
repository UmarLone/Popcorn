using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Popcorn.Entity;
using Popcorn.Entity.Cast;
using Popcorn.Entity.Movie;
using Popcorn.Models.Cast;
using Popcorn.Models.Genres;
using Popcorn.Models.Movie;
using Popcorn.Models.Torrent.Movie;

namespace Popcorn.Services.Movies.History
{
    /// <summary>
    /// Services used to interact with movie history
    /// </summary>
    public class MovieHistoryService : IMovieHistoryService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Set if movies have been seen or set as favorite
        /// </summary>
        /// <param name="movies">All movies to compute</param>
        public async Task SetMovieHistoryAsync(IEnumerable<MovieJson> movies)
        {
            if (movies == null) throw new ArgumentNullException(nameof(movies));
            var watch = Stopwatch.StartNew();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var history = await context.MovieHistory.FirstOrDefaultAsync();
                    if (history == null)
                    {
                        await CreateMovieHistoryAsync();
                        history = await context.MovieHistory.FirstOrDefaultAsync();
                    }

                    foreach (var movie in movies)
                    {
                        var entityMovie = history.Movies.FirstOrDefault(p => p.ImdbCode == movie.ImdbCode);
                        if (entityMovie == null) continue;
                        movie.IsFavorite = entityMovie.IsFavorite;
                        movie.HasBeenSeen = entityMovie.HasBeenSeen;
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"ComputeMovieHistoryAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"ComputeMovieHistoryAsync in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Get the favorites movies
        /// </summary>
        /// <param name="genre">The genre of the movies</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Favorites movies</returns>
        public async Task<IEnumerable<MovieJson>> GetFavoritesMoviesAsync(GenreJson genre, double ratingFilter)
        {
            var watch = Stopwatch.StartNew();

            var movies = new List<MovieJson>();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    if (movieHistory == null)
                        return movies;

                    if (genre != null)
                    {
                        movies.AddRange(movieHistory.Movies.Where(
                                p =>
                                    p.IsFavorite && p.Genres.Any(g => g.Name == genre.EnglishName) &&
                                    p.Rating >= ratingFilter)
                            .Select(MovieEntityToMovieJson));
                    }
                    else
                    {
                        movies.AddRange(movieHistory.Movies.Where(
                                p => p.IsFavorite)
                            .Select(MovieEntityToMovieJson));
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetFavoritesMoviesIdAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetFavoritesMoviesIdAsync in {elapsedMs} milliseconds.");
            }

            return movies;
        }

        /// <summary>
        /// Get the seen movies
        /// </summary>
        /// <returns>Seen movies</returns>
        /// <param name="genre">The genre of the movies</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Seen movies</returns>
        public async Task<IEnumerable<MovieJson>> GetSeenMoviesAsync(GenreJson genre, double ratingFilter)
        {
            var watch = Stopwatch.StartNew();

            var movies = new List<MovieJson>();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    if (movieHistory == null)
                        return movies;

                    if (genre != null)
                    {
                        movies.AddRange(movieHistory.Movies.Where(
                                p =>
                                    p.HasBeenSeen && p.Genres.Any(g => g.Name == genre.EnglishName) &&
                                    p.Rating >= ratingFilter)
                            .Select(MovieEntityToMovieJson));
                    }
                    else
                    {
                        movies.AddRange(movieHistory.Movies.Where(
                                p => p.HasBeenSeen)
                            .Select(MovieEntityToMovieJson));
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetSeenMoviesIdAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetSeenMoviesIdAsync in {elapsedMs} milliseconds.");
            }

            return movies;
        }

        /// <summary>
        /// Set the movie as favorite
        /// </summary>
        /// <param name="movie">Favorite movie</param>
        public async Task SetFavoriteMovieAsync(MovieJson movie)
        {
            if (movie == null) throw new ArgumentNullException(nameof(movie));
            var watch = Stopwatch.StartNew();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    if (movieHistory == null)
                    {
                        await CreateMovieHistoryAsync();
                        movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    }

                    if (movieHistory.Movies == null)
                    {
                        movieHistory.Movies = new List<MovieEntity>
                        {
                            MovieJsonToEntity(movie)
                        };

                        context.MovieHistory.AddOrUpdate(movieHistory);
                    }
                    else
                    {
                        var movieShort = movieHistory.Movies.FirstOrDefault(p => p.ImdbCode == movie.ImdbCode);
                        if (movieShort == null)
                            movieHistory.Movies.Add(MovieJsonToEntity(movie));
                        else
                            movieShort.IsFavorite = movie.IsFavorite;
                    }

                    await context.SaveChangesAsync();
                }
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
        /// Set a movie as seen
        /// </summary>
        /// <param name="movie">Seen movie</param>
        public async Task SetHasBeenSeenMovieAsync(MovieJson movie)
        {
            if (movie == null) throw new ArgumentNullException(nameof(movie));
            var watch = Stopwatch.StartNew();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    if (movieHistory == null)
                    {
                        await CreateMovieHistoryAsync();
                        movieHistory = await context.MovieHistory.FirstOrDefaultAsync();
                    }

                    if (movieHistory.Movies == null)
                    {
                        movieHistory.Movies = new List<Entity.Movie.MovieEntity>
                        {
                            MovieJsonToEntity(movie)
                        };

                        context.MovieHistory.AddOrUpdate(movieHistory);
                    }
                    else
                    {
                        var movieFull = movieHistory.Movies.FirstOrDefault(p => p.ImdbCode == movie.ImdbCode);
                        if (movieFull == null)
                            movieHistory.Movies.Add(MovieJsonToEntity(movie));
                        else
                            movieFull.HasBeenSeen = movie.HasBeenSeen;
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SetHasBeenSeenMovieAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SetHasBeenSeenMovieAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Scaffold movie history if empty
        /// </summary>
        private static async Task CreateMovieHistoryAsync()
        {
            var watch = Stopwatch.StartNew();

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var userData = await context.MovieHistory.FirstOrDefaultAsync();
                    if (userData == null)
                    {
                        context.MovieHistory.AddOrUpdate(new MovieHistory
                        {
                            Created = DateTime.Now,
                            Movies = new List<MovieEntity>()
                        });

                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"CreateMovieHistoryAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"CreateMovieHistoryAsync in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Convert a movie entity to a json movie
        /// </summary>
        /// <param name="movie">The movie to convert</param>
        /// <returns>Json movie</returns>
        private static MovieJson MovieEntityToMovieJson(MovieEntity movie)
        {
            if (movie == null) throw new ArgumentNullException(nameof(movie));
            var torrents = movie.Torrents.Select(torrent => new TorrentJson
            {
                DateUploaded = torrent.DateUploaded,
                Url = torrent.Url,
                Quality = torrent.Quality,
                DateUploadedUnix = torrent.DateUploadedUnix,
                Hash = torrent.Hash,
                Peers = torrent.Peers,
                Seeds = torrent.Seeds,
                Size = torrent.Size,
                SizeBytes = torrent.SizeBytes
            });

            var genres = movie.Genres.Select(genre => genre.Name);

            var cast = movie.Cast.Select(actor => new CastJson
            {
                CharacterName = actor.CharacterName,
                Name = actor.Name,
                SmallImage = actor.SmallImage,
                ImdbCode = actor.ImdbCode
            });

            var movieFull = new MovieJson
            {
                Year = movie.Year,
                Language = movie.Language,
                ImdbCode = movie.ImdbCode,
                Title = movie.Title,
                DateUploaded = movie.DateUploaded,
                Runtime = movie.Runtime,
                Url = movie.Url,
                TitleLong = movie.TitleLong,
                Torrents = torrents.ToList(),
                Genres = genres.ToList(),
                DateUploadedUnix = movie.DateUploadedUnix,
                MpaRating = movie.MpaRating,
                Rating = movie.Rating,
                DescriptionFull = movie.DescriptionFull,
                Cast = cast.ToList(),
                DescriptionIntro = movie.DescriptionIntro,
                DownloadCount = movie.DownloadCount,
                Slug = movie.Slug,
                PosterImage = movie.PosterImage,
                BackdropImage = movie.BackdropImage,
                LikeCount = movie.LikeCount,
                YtTrailerCode = movie.YtTrailerCode,
                HasBeenSeen = movie.HasBeenSeen,
                IsFavorite = movie.IsFavorite,
                BackgroundImage = movie.BackgroundImage,
                MediumCoverImage = movie.MediumCoverImage,
                SmallCoverImage = movie.SmallCoverImage,
                LargeCoverImage = movie.LargeCoverImage,
                LargeScreenshotImage1 = movie.LargeScreenshotImage1,
                LargeScreenshotImage2 = movie.LargeScreenshotImage2,
                LargeScreenshotImage3 = movie.MediumScreenshotImage3,
                MediumScreenshotImage3 = movie.MediumScreenshotImage3,
                MediumScreenshotImage1 = movie.MediumScreenshotImage1,
                MediumScreenshotImage2 = movie.MediumScreenshotImage2
            };

            return movieFull;
        }

        /// <summary>
        /// Convert a json movie to an entity
        /// </summary>
        /// <param name="movie">The movie to convert</param>
        /// <returns>Full movie entity</returns>
        private static MovieEntity MovieJsonToEntity(MovieJson movie)
        {
            if (movie == null) throw new ArgumentNullException(nameof(movie));
            var torrents = movie.Torrents.Select(torrent => new Torrent
            {
                DateUploaded = torrent.DateUploaded,
                Url = torrent.Url,
                Quality = torrent.Quality,
                DateUploadedUnix = torrent.DateUploadedUnix,
                Hash = torrent.Hash,
                Peers = torrent.Peers,
                Seeds = torrent.Seeds,
                Size = torrent.Size,
                SizeBytes = torrent.SizeBytes
            });

            var genres = movie.Genres.Select(genre => new Genre
            {
                Name = genre
            });

            var cast = movie.Cast.Select(actor => new Cast
            {
                CharacterName = actor.CharacterName,
                Name = actor.Name,
                SmallImage = actor.SmallImage,
                ImdbCode = actor.ImdbCode
            });

            var movieFull = new MovieEntity
            {
                Year = movie.Year,
                Language = movie.Language,
                ImdbCode = movie.ImdbCode,
                Title = movie.Title,
                DateUploaded = movie.DateUploaded,
                Runtime = movie.Runtime,
                Url = movie.Url,
                TitleLong = movie.TitleLong,
                Torrents = torrents.ToList(),
                Genres = genres.ToList(),
                DateUploadedUnix = movie.DateUploadedUnix,
                MpaRating = movie.MpaRating,
                Rating = movie.Rating,
                DescriptionFull = movie.DescriptionFull,
                Cast = cast.ToList(),
                DescriptionIntro = movie.DescriptionIntro,
                DownloadCount = movie.DownloadCount,
                LikeCount = movie.LikeCount,
                YtTrailerCode = movie.YtTrailerCode,
                HasBeenSeen = movie.HasBeenSeen,
                IsFavorite = movie.IsFavorite,
                BackgroundImage = movie.BackgroundImage,
                MediumCoverImage = movie.MediumCoverImage,
                SmallCoverImage = movie.SmallCoverImage,
                LargeCoverImage = movie.LargeCoverImage,
                LargeScreenshotImage1 = movie.LargeScreenshotImage1,
                LargeScreenshotImage2 = movie.LargeScreenshotImage2,
                LargeScreenshotImage3 = movie.MediumScreenshotImage3,
                MediumScreenshotImage3 = movie.MediumScreenshotImage3,
                MediumScreenshotImage1 = movie.MediumScreenshotImage1,
                MediumScreenshotImage2 = movie.MediumScreenshotImage2,
                Slug = movie.Slug,
                BackdropImage = movie.BackdropImage,
                PosterImage = movie.PosterImage
            };

            return movieFull;
        }
    }
}