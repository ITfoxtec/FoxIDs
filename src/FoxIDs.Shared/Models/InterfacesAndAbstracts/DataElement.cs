using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public abstract class DataElement : IDataElement
    {
        [Required]
        [JsonProperty(PropertyName = "id")]
        public abstract string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "data_type")]
        public virtual string DataType { get; set; }
    }
}
