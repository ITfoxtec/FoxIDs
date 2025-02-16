using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public abstract class DataElement : IDataElement
    {
        [Required]
        [JsonProperty(PropertyName = "id")]
        public abstract string Id { get; set; }

        [ListLength(0, 0, 0)]
        [JsonProperty(PropertyName = "a_ids")]
        public virtual List<string> AdditionalIds { get; set; }

        [Required]
        [JsonProperty(PropertyName = "data_type")]
        public virtual string DataType { get; set; }
    }
}
