using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Popcorn.Models.Episode;
using Popcorn.Models.Image;
using Popcorn.Models.Rating;
using RestSharp.Deserializers;

namespace Popcorn.Models.Anime
{
    public class AnimeJson
    {
        [DeserializeAs(Name = "mal_id")]
        public string MalId { get; set; }

        [DeserializeAs(Name = "title")]
        public string Title { get; set; }

        [DeserializeAs(Name = "year")]
        public int Year { get; set; }

        [DeserializeAs(Name = "slug")]
        public string Slug { get; set; }

        [DeserializeAs(Name = "synopsis")]
        public string Synopsis { get; set; }

        [DeserializeAs(Name = "runtime")]
        public string Runtime { get; set; }

        [DeserializeAs(Name = "status")]
        public string Status { get; set; }

        [DeserializeAs(Name = "type")]
        public string Type { get; set; }

        [DeserializeAs(Name = "last_updated")]
        public long LastUpdated { get; set; }

        [DeserializeAs(Name = "num_seasons")]
        public int NumSeasons { get; set; }

        [DeserializeAs(Name = "episodes")]
        public List<EpisodeAnimeJson> Episodes { get; set; }

        [DeserializeAs(Name = "genres")]
        public List<string> Genres { get; set; }

        [DeserializeAs(Name = "images")]
        public ImageAnimeJson Images { get; set; }

        [DeserializeAs(Name = "rating")]
        public RatingJson Rating { get; set; }
    }
}
