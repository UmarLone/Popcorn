using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.Image
{
    public class ImageAnimeJson
    {
        [DeserializeAs(Name = "poster_kitsu")]
        public ImageAnimeTypeJson Poster { get; set; }

        [DeserializeAs(Name = "cover_kitsu")]
        public ImageAnimeTypeJson Cover { get; set; }
    }

    public class ImageAnimeTypeJson
    {
        [DeserializeAs(Name = "tiny")]
        public string Tiny { get; set; }

        [DeserializeAs(Name = "small")]
        public string Small { get; set; }

        [DeserializeAs(Name = "medium")]
        public string Medium { get; set; }

        [DeserializeAs(Name = "large")]
        public string Large { get; set; }

        [DeserializeAs(Name = "original")]
        public string Original { get; set; }
    }
}
