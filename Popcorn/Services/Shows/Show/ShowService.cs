using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Popcorn.Helpers;
using Popcorn.Models.Genre;
using Popcorn.Models.Movie;
using Popcorn.Models.Shows;
using RestSharp;

namespace Popcorn.Services.Shows.Show
{
    public class ShowService : IShowService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Get popular shows by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of shows to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular shows and the number of shows found</returns>
        public async Task<Tuple<IEnumerable<ShowJson>, int>> GetPopularShowsAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var watch = Stopwatch.StartNew();

            var wrapper = new ShowResponse();

            if (limit < 1 || limit > 50)
                limit = Constants.MaxShowsPerPage;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Constants.PopcornApi);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "shows");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("sort_by", "votes");

            try
            {
                var response = await restClient.ExecuteGetTaskAsync<ShowResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetPopularShowsAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetPopularShowsAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetPopularShowsAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var shows = wrapper.Shows ?? new List<ShowJson>();

            var nbShows = wrapper.TotalShows;

            return new Tuple<IEnumerable<ShowJson>, int>(shows, nbShows);
        }
    }
}