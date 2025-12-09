using ITfoxtec.Identity.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to create or upload contained track keys.
    /// </summary>
    public class TrackKeyItemContainedRequest : IValidatableObject 
    {
        /// <summary>
        /// Indicates if the key should be promoted to primary.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Generate a self-signed key if true.
        /// </summary>
        public bool CreateSelfSigned { get; set; }

        /// <summary>
        /// Provided key material when not generating self-signed.
        /// </summary>
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
