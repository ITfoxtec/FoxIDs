using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class SamlUpSequenceData : UpSequenceData
    {
        [JsonProperty(PropertyName = "i")]
        public string Id { get; set; }

        [MaxLength(2000)]
        [JsonProperty(PropertyName = "rs")]
        public string RelayState { get; set; }

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

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>(base.Validate(validationContext));

            if (ExternalInitiatedSingleLogout)
            {
                if (Id.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"The field {nameof(Id)} is required if external initiated single logout.", new[] { nameof(Id) }));
                }
            }

            return results;
        }
    }
}
