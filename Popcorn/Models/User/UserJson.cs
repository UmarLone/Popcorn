using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.User
{
    public class UserJson
    {
        [DeserializeAs(Name = "MachineGuid")]
        public Guid MachineGuid { get; set; }

        [DeserializeAs(Name = "DownloadLimit")]
        public int DownloadLimit { get; set; }

        [DeserializeAs(Name = "UploadLimit")]
        public int UploadLimit { get; set; }

        [DeserializeAs(Name = "Language")]
        public LanguageJson Language { get; set; }

        [DeserializeAs(Name = "MovieHistory")]
        public List<MovieHistoryJson> MovieHistory { get; set; }

        [DeserializeAs(Name = "ShowHistory")]
        public List<ShowHistoryJson> ShowHistory { get; set; }
    }
}
