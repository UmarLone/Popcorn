using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.Anime
{
    public class AnimeResponse
    {
        [DeserializeAs(Name = "totalAnimes")]
        public int TotalAnimes { get; set; }

        [DeserializeAs(Name = "animes")]
        public List<AnimeJson> Animes { get; set; }
    }
}
