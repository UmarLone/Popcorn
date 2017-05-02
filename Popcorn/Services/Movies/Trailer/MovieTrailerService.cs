using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Exceptions;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Movie;
using Popcorn.Services.Movies.Movie;

namespace Popcorn.Services.Movies.Trailer
{
    /// <summary>
    /// Manage trailer
    /// </summary>
    public sealed class MovieTrailerService : IMovieTrailerService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The service used to interact with movies
        /// </summary>
        private readonly IMovieService _movieService;

        /// <summary>
        /// Initializes a new instance of the TrailerViewModel class.
        /// </summary>
        /// <param name="movieService">Movie service</param>
        public MovieTrailerService(IMovieService movieService)
        {
            _movieService = movieService;
        }

        /// <summary>
        /// Load movie's trailer asynchronously
        /// </summary>
        /// <param name="movie">The movie</param>
        /// <param name="ct">Cancellation token</param>
        public async Task LoadTrailerAsync(MovieJson movie, CancellationToken ct)
        {
            try
            {
                var trailer = await _movieService.GetMovieTrailerAsync(movie, ct);
                var trailerUrl = await _movieService.GetVideoTrailerUrlAsync(trailer.Results.FirstOrDefault()?.Key, ct);

                if (string.IsNullOrEmpty(trailerUrl))
                {
                    Logger.Error(
                        $"Failed loading movie's trailer: {movie.Title}");
                    Messenger.Default.Send(
                        new ManageExceptionMessage(
                            new PopcornException(
                                LocalizationProviderHelper.GetLocalizedValue<string>("TrailerNotAvailable"))));
                    Messenger.Default.Send(new StopPlayingTrailerMessage());
                    return;
                }

                if (!ct.IsCancellationRequested)
                {
                    Logger.Debug(
                        $"Movie's trailer loaded: {movie.Title}");
                    Messenger.Default.Send(new PlayTrailerMessage(trailerUrl, movie.Title, () =>
                        {
                            Messenger.Default.Send(new StopPlayingTrailerMessage());
                        },
                        () =>
                        {
                            Messenger.Default.Send(new StopPlayingTrailerMessage());
                        }));
                }
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetMovieTrailerAsync cancelled.");
                Messenger.Default.Send(new StopPlayingTrailerMessage());
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetMovieTrailerAsync: {exception.Message}");
                Messenger.Default.Send(
                    new ManageExceptionMessage(
                        new PopcornException(
                            LocalizationProviderHelper.GetLocalizedValue<string>(
                                "TrailerNotAvailable"))));
                Messenger.Default.Send(new StopPlayingTrailerMessage());
            }
        }
    }
}