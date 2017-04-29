using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Popcorn.Models.Genres;
using Popcorn.Models.Shows;

namespace Popcorn.Services.Shows.Show
{
    public interface IShowService
    {
        /// <summary>
        /// Get popular shows by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of shows to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular shows and the number of shows found</returns>
        Task<Tuple<IEnumerable<ShowJson>, int>> GetPopularShowsAsync(int page,
            int limit,
            double ratingFilter,
            CancellationToken ct,
            GenreJson genre = null);
    }
}
