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

namespace Popcorn.Models.Shows
{
    public class ShowJson
    {
        [DeserializeAs(Name = "imdb_id")]
        public string ImdbId { get; set; }

        [DeserializeAs(Name = "tvdb_id")]
        public string TvdbId { get; set; }

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

        [DeserializeAs(Name = "country")]
        public string Country { get; set; }

        [DeserializeAs(Name = "network")]
        public string Network { get; set; }

        [DeserializeAs(Name = "air_day")]
        public string AirDay { get; set; }

        [DeserializeAs(Name = "air_time")]
        public string AirTime { get; set; }

        [DeserializeAs(Name = "status")]
        public string Status { get; set; }

        [DeserializeAs(Name = "num_seasons")]
        public int NumSeasons { get; set; }

        [DeserializeAs(Name = "last_updated")]
        public long LastUpdated { get; set; }

        [DeserializeAs(Name = "episodes")]
        public List<EpisodeShowJson> Episodes { get; set; }

        [DeserializeAs(Name = "genres")]
        public List<string> Genres { get; set; }

        [DeserializeAs(Name = "images")]
        public ImageShowJson Images { get; set; }

        [DeserializeAs(Name = "rating")]
        public RatingJson Rating { get; set; }

        [DeserializeAs(Name = "similar")]
        public List<string> Similars { get; set; }
    }
}
