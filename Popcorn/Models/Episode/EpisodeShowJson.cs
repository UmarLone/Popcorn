using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Popcorn.Models.Torrent.Show;

namespace Popcorn.Models.Episode
{
    public class EpisodeShowJson
    {
        [JsonProperty("torrents")]
        public TorrentShowNodeJson Torrents { get; set; }

        [JsonProperty("first_aired")]
        public long FirstAired { get; set; }

        [JsonProperty("date_based")]
        public bool DateBased { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("episode")]
        public int EpisodeNumber { get; set; }

        [JsonProperty("season")]
        public int Season { get; set; }

        [JsonProperty("tvdb_id")]
        public int? TvdbId { get; set; }
    }
}
