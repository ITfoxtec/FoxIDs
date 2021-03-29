using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class UpSequenceData : ISequenceData, IValidatableObject
    {
        [JsonProperty(PropertyName = "es")]
        public bool ExternalInitiatedSingleLogout { get; set; } = false;

        [JsonProperty(PropertyName = "di")]
        public string DownPartyId { get; set; }

        [JsonProperty(PropertyName = "dt")]
        public PartyTypes? DownPartyType { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ui")]
        public string UpPartyId { get; set; }

        [JsonProperty(PropertyName = "la")]
        public LoginAction LoginAction { get; set; }

        [JsonProperty(PropertyName = "u")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "ma")]
        public int? MaxAge { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!ExternalInitiatedSingleLogout)
            {
                if (DownPartyId.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(DownPartyId)} is required if not external initiated single logout.", new[] { nameof(DownPartyId) }));
                }
                if (!DownPartyType.HasValue)
                {
                    results.Add(new ValidationResult($"The field {nameof(DownPartyType)} is required if not external initiated single logout.", new[] { nameof(DownPartyType) }));
                }
            }

            return results;
        }
    }
}
