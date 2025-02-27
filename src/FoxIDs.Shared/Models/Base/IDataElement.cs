using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface IDataElement
    {
        [JsonProperty(PropertyName = "id")]
        string Id { get; set; }

        [JsonProperty(PropertyName = "a_ids")]
        List<string> AdditionalIds { get; set; }

        [JsonProperty(PropertyName = "data_type")]
        string DataType { get; set; }
    }
}
