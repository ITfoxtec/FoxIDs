using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthDownScope : OAuthDownScope<OAuthDownClaim> { }
    public class OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        [Required]
        [MaxLength(50)]
        [JsonProperty(PropertyName = "scope")]
        public string Scope { get; set; }

        [Length(0, 100)]
        [JsonProperty(PropertyName = "voluntary_claims")]
        public List<TClaim> VoluntaryClaims { get; set; }
    }
}
