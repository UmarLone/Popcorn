using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Popcorn.Models.Anime
{
    public class AnimeResponse
    {
        [JsonProperty("totalAnimes")]
        public int TotalAnimes { get; set; }

        [JsonProperty("animes")]
        public List<AnimeJson> Animes { get; set; }
    }
}
