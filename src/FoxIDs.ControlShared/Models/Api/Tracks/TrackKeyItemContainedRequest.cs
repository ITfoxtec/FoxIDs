using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TrackKeyItemContainedRequest : IValidatableObject 
    {
        public bool IsPrimary { get; set; }

        public bool CreateSelfSigned { get; set; }

        public JsonWebKey Key { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (!CreateSelfSigned && Key == null)
            {
                results.Add(new ValidationResult($"The field {nameof(Key)} is required if {nameof(CreateSelfSigned)} is false.", new[] { nameof(Key) }));
            }
            return results;
        }
    }
}
