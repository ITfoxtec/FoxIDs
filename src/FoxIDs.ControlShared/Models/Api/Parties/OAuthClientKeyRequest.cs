using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// OAuth client key import request.
    /// </summary>
    public class OAuthClientKeyRequest : IValidatableObject
    {
        /// <summary>
        /// OAuth 2.0 party name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string PartyName { get; set; }

        [Required]
        public ClientKeyTypes Type { get; set; }

        /// <summary>
        /// Base64 url encode certificate.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Certificate.EncodeCertificateLength)]
        public string Certificate { get; set; }

        /// <summary>
        /// Certificate password
        /// </summary>
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        public string Password { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type != ClientKeyTypes.KeyVaultImport)
            {
                results.Add(new ValidationResult($"Only the type '{ClientKeyTypes.KeyVaultImport}' is supported.", new[] { nameof(Type) }));
            }
            return results;
        }
    }
}
