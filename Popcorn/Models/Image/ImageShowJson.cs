using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Popcorn.Models.Image
{
    public class ImageShowJson
    {
        [JsonProperty("poster")]
        public string Poster { get; set; }

        [JsonProperty("fanart")]
        public string Fanart { get; set; }

        [JsonProperty("banner")]
        public string Banner { get; set; }
    }
}
