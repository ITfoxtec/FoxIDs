using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ClaimAndValues
    {
        [Required]
        [MaxLength(50)]
        [JsonProperty(PropertyName = "claim")]
        public string Claim { get; set; }

        [Length(1, Constants.LengthDefinitions.JwtClaimValue, 1000)]
        [JsonProperty(PropertyName = "values")]
        public List<string> Values { get; set; }
    }
}
