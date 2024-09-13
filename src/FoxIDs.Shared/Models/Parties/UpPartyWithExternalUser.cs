using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class UpPartyWithExternalUser<TProfile> : UpPartyWithProfile<TProfile>, IValidatableObject where TProfile : UpPartyProfile
    {
        [ValidateComplexType]
        [JsonProperty(PropertyName = "link_external_user")]
        public LinkExternalUser LinkExternalUser { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }
            return results;
        }
    }
}
