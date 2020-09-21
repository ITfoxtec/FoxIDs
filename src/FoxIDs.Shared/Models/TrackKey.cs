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
        public TrackKeyType Type { get; set; }

        [JsonProperty(PropertyName = "external_name")]
        public string ExternalName { get; set; }

        [Length(Constants.Models.Track.KeysMin, Constants.Models.Track.KeysMax)]
        [JsonProperty(PropertyName = "keys")]
        public List<TrackKeyItem> Keys { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type == TrackKeyType.KeyVaultRenewSelfSigned && ExternalName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(ExternalName)} is required for track key type '{Type}'.", new[] { nameof(ExternalName) }));
            }
            else if(Type != TrackKeyType.KeyVaultRenewSelfSigned && !ExternalName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(ExternalName)} is not supported for track key type '{Type}'.", new[] { nameof(ExternalName) }));
            }

            if(Type == TrackKeyType.Contained && Keys?.Count < 1) 
            {
                results.Add(new ValidationResult($"The field {nameof(Keys)} required at least one element for track key type '{Type}'.", new[] { nameof(Keys) }));
            }
            else if (Type != TrackKeyType.Contained && Keys?.Count > 0)
            {
                results.Add(new ValidationResult($"The field {nameof(Keys)} is not supported for track key type '{Type}'.", new[] { nameof(Keys) }));
            }

            return results;
        }
    }
}
