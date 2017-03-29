using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Popcorn.Models.Shows
{
    public class ShowResponse
    {
        [JsonProperty("totalShows")]
        public int TotalShows { get; set; }

        [JsonProperty("shows")]
        public List<ShowJson> Shows { get; set; }
    }
}
