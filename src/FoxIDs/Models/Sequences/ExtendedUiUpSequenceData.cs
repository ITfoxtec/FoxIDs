using FoxIDs.Models.Logic;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class ExtendedUiUpSequenceData : UpSequenceData
    {
        public ExtendedUiUpSequenceData() : base() { }

        public ExtendedUiUpSequenceData(ILoginRequest loginRequest) : base(loginRequest) { }

        [Required]
        [JsonProperty(PropertyName = "ut")]
        public PartyTypes UpPartyType { get; set; }

        [JsonProperty(PropertyName = "st")]
        public List<ExtendedUiStep> Steps { get; set; } = new List<ExtendedUiStep>();

        [JsonProperty(PropertyName = "esi")]
        public string ExternalSessionId { get; set; }

        [JsonProperty(PropertyName = "it")]
        public string IdToken { get; set; }
    }
}
