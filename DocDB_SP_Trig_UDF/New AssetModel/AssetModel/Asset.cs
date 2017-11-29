using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetModel
{
    public class Asset
    {
        [JsonProperty(PropertyName = "id")]
        public string AssetId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string RootId { get; set; }
        public string AssetType { get; set; }
        public string Location { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public IList<Tag> TagCollection { get; set; }
        public DataSource DataSource { get; set; }
    }
}
