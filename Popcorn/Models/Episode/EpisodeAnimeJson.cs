using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Popcorn.Models.Torrent.Show;
using RestSharp.Deserializers;

namespace Popcorn.Models.Episode
{
    public class EpisodeAnimeJson
    {
        [DeserializeAs(Name = "torrents")]
        public TorrentShowNodeJson Torrents { get; set; }

        [DeserializeAs(Name = "overview")]
        public string Overview { get; set; }

        [DeserializeAs(Name = "title")]
        public string Title { get; set; }

        [DeserializeAs(Name = "episode")]
        public int EpisodeNumber { get; set; }

        [DeserializeAs(Name = "season")]
        public int Season { get; set; }

        [DeserializeAs(Name = "tvdb_id")]
        public string TvdbId { get; set; }
    }
}
