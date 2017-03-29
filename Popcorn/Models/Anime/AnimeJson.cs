using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Popcorn.Models.Episode;
using Popcorn.Models.Image;
using Popcorn.Models.Rating;

namespace Popcorn.Models.Anime
{
    public class AnimeJson
    {
        [JsonProperty("mal_id")]
        public string MalId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("year")]
        public int Year { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("synopsis")]
        public string Synopsis { get; set; }

        [JsonProperty("runtime")]
        public string Runtime { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("last_updated")]
        public long LastUpdated { get; set; }

        [JsonProperty("num_seasons")]
        public int NumSeasons { get; set; }

        [JsonProperty("episodes")]
        public List<EpisodeAnimeJson> Episodes { get; set; }

        [JsonProperty("genres")]
        public List<string> Genres { get; set; }

        [JsonProperty("images")]
        public ImageAnimeJson Images { get; set; }

        [JsonProperty("rating")]
        public RatingJson Rating { get; set; }
    }
}
