using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class SamlUpSequenceData : UpSequenceData
    {
        [MaxLength(200)]
        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public bool RequireLogoutConsent { get; set; }

        [JsonProperty(PropertyName = "lr")]
        public bool PostLogoutRedirect { get; set; }

        [Length(0, 10)]
        [JsonProperty(PropertyName = "c")]
        public List<ClaimAndValues> Claims { get; set; }
    }
}
