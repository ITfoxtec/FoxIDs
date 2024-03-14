using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class CreateExternalUserRequest 
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        public string UpPartyName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string LinkClaim { get; set; }

        [ListLength(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        [JsonProperty(PropertyName = "claims")]
        public List<ClaimAndValues> Claims { get; set; }
    }
}
