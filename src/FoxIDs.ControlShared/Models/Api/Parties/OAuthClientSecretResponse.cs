using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// OAuth 2.0 client secret response.
    /// </summary>
    public class OAuthClientSecretResponse : INameValue
    {
        /// <summary>
        /// Secret name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength + Constants.Models.SecretHash.IdLength + 1)]
        [RegularExpression(Constants.Models.Party.NameAndGuidIdRegExPattern)]
        public string Name { get; set; }

        /// <summary>
        /// Secret info.
        /// </summary>
        [MaxLength(Constants.Models.SecretHash.InfoLength)]
        public string Info { get; set; }
    }
}
