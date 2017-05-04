﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Popcorn.Models.Genres;
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
        /// Get show by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Show's Imdb code</param>
        /// <returns>The show</returns>
        public async Task<ShowJson> GetShowAsync(string imdbCode)
        {
            var watch = Stopwatch.StartNew();
            var restClient = new RestClient(Utils.Constants.PopcornApi);
            var request = new RestRequest("/{segment}/{show}", Method.GET);
            request.AddUrlSegment("segment", "shows");
            request.AddUrlSegment("show", imdbCode);
            var show = new ShowJson();
            try
            {
                var response = await restClient.ExecuteTaskAsync<ShowJson>(request);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                show = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetShowAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetShowAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetShowAsync ({imdbCode}) in {elapsedMs} milliseconds.");
            }

            return show;
        }

        /// <summary>
        /// Get popular shows by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of shows to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="sortBy">The sort</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular shows and the number of shows found</returns>
        public async Task<(IEnumerable<ShowJson> shows, int nbShows)> GetShowsAsync(int page,
            int limit,
            double ratingFilter,
            string sortBy,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var watch = Stopwatch.StartNew();
            var wrapper = new ShowResponse();
            if (limit < 1 || limit > 50)
                limit = Utils.Constants.MaxShowsPerPage;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Utils.Constants.PopcornApi);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "shows");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("sort_by", sortBy);
            try
            {
                var response = await restClient.ExecuteTaskAsync<ShowResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetShowsAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetShowsAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetShowsAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var shows = wrapper?.Shows ?? new List<ShowJson>();
            var nbShows = wrapper?.TotalShows ?? 0;
            return (shows, nbShows);
        }

        /// <summary>
        /// Search shows by criteria
        /// </summary>
        /// <param name="criteria">Criteria used for search</param>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Searched shows and the number of movies found</returns>
        public async Task<(IEnumerable<ShowJson> shows, int nbShows)> SearchShowsAsync(string criteria,
            int page,
            int limit,
            GenreJson genre,
            double ratingFilter,
            CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();
            var wrapper = new ShowResponse();
            if (limit < 1 || limit > 50)
                limit = Utils.Constants.MaxShowsPerPage;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Utils.Constants.PopcornApi);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "shows");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("query_term", criteria);
            try
            {
                var response = await restClient.ExecuteTaskAsync<ShowResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "SearchShowsAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SearchShowsAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SearchShowsAsync ({criteria}, {page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var result = wrapper?.Shows ?? new List<ShowJson>();
            var nbResult = wrapper?.TotalShows ?? 0;
            return (result, nbResult);
        }
    }
}