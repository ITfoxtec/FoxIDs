using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Sequences
{
    public class FormActionSequenceData : ISequenceData
    {
        [Length(0, 10, 200)]
        [JsonProperty(PropertyName = "d")]
        public List<string> Domains { get; set; }
    }
}
