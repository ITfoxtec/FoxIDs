using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ExternalUserId
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        public string UpPartyName { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string LinkClaimValue { get; set; }
    }
}
