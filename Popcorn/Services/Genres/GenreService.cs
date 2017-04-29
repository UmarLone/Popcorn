using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Popcorn.Models.Genres;

namespace Popcorn.Services.Genres
{
    public class GenreService : IGenreService
    {
        /// <summary>
        /// Get all genres
        /// </summary>
        /// <param name="language">Genre language</param>
        /// <param name="ct">Used to cancel loading genres</param>
        /// <returns>Genres</returns>
        public async Task<List<GenreJson>> GetGenresAsync(string language, CancellationToken ct)
        {
            var response = new GenreResponse
            {
                Genres = new List<GenreJson>
                {
                    new GenreJson
                    {
                        EnglishName = "Action",
                        Name = language == "fr" ? "Action" : "Action"
                    },
                    new GenreJson
                    {
                        EnglishName = "Adventure",
                        Name = language == "fr" ? "Aventure" : "Adventure"
                    },
                    new GenreJson
                    {
                        EnglishName = "Animation",
                        Name = language == "fr" ? "Animation" : "Animation"
                    },
                    new GenreJson
                    {
                        EnglishName = "Comedy",
                        Name = language == "fr" ? "Comédie" : "Comedy"
                    },
                    new GenreJson
                    {
                        EnglishName = "Crime",
                        Name = language == "fr" ? "Crime" : "Crime"
                    },
                    new GenreJson
                    {
                        EnglishName = "Documentary",
                        Name = language == "fr" ? "Documentaire" : "Documentary"
                    },
                    new GenreJson
                    {
                        EnglishName = "Drama",
                        Name = language == "fr" ? "Drame" : "Drama"
                    },
                    new GenreJson
                    {
                        EnglishName = "Family",
                        Name = language == "fr" ? "Familial" : "Family"
                    },
                    new GenreJson
                    {
                        EnglishName = "Fantasy",
                        Name = language == "fr" ? "Fantastique" : "Fantasy"
                    },
                    new GenreJson
                    {
                        EnglishName = "History",
                        Name = language == "fr" ? "Histoire" : "History"
                    },
                    new GenreJson
                    {
                        EnglishName = "Horror",
                        Name = language == "fr" ? "Horreur" : "Horror"
                    },
                    new GenreJson
                    {
                        EnglishName = "Music",
                        Name = language == "fr" ? "Musique" : "Music"
                    },
                    new GenreJson
                    {
                        EnglishName = "Mystery",
                        Name = language == "fr" ? "Mystère" : "Mystery"
                    },
                    new GenreJson
                    {
                        EnglishName = "Romance",
                        Name = language == "fr" ? "Romance" : "Romance"
                    },
                    new GenreJson
                    {
                        EnglishName = "Science Fiction",
                        Name = language == "fr" ? "Science-Fiction" : "Science Fiction"
                    },
                    new GenreJson
                    {
                        EnglishName = "TV Movie",
                        Name = language == "fr" ? "Téléfilm" : "TV Movie"
                    },
                    new GenreJson
                    {
                        EnglishName = "Thriller",
                        Name = language == "fr" ? "Thriller" : "Thriller"
                    },
                    new GenreJson
                    {
                        EnglishName = "War",
                        Name = language == "fr" ? "Guerre" : "War"
                    },
                    new GenreJson
                    {
                        EnglishName = "Western",
                        Name = language == "fr" ? "Western" : "Western"
                    },
                }
            };

            return await Task.FromResult(response.Genres);
        }
    }
}