using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class TrackKey : IValidatableObject
    {
        [Required]
        [JsonProperty(PropertyName = "type")]
        public TrackKeyTypes Type { get; set; }

        [JsonProperty(PropertyName = "external_name")]
        public string ExternalName { get; set; }

        [ListLength(Constants.Models.Track.KeysMin, Constants.Models.Track.KeysMax)]
        [JsonProperty(PropertyName = "keys")]
        public List<TrackKeyItem> Keys { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type == TrackKeyTypes.KeyVaultRenewSelfSigned && ExternalName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(ExternalName)} is required for environment key type '{Type}'.", new[] { nameof(ExternalName) }));
            }
            else if(Type != TrackKeyTypes.KeyVaultRenewSelfSigned && !ExternalName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(ExternalName)} is not supported for environment key type '{Type}'.", new[] { nameof(ExternalName) }));
            }

            if(Type == TrackKeyTypes.Contained && Keys?.Count < 1) 
            {
                results.Add(new ValidationResult($"The field {nameof(Keys)} required at least one element for environment key type '{Type}'.", new[] { nameof(Keys) }));
            }
            else if (Type != TrackKeyTypes.Contained && Keys?.Count > 0)
            {
                results.Add(new ValidationResult($"The field {nameof(Keys)} is not supported for environment key type '{Type}'.", new[] { nameof(Keys) }));
            }

            return results;
        }
    }
}
