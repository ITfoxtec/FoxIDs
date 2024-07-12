using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// External login secret request .
    /// </summary>
    public class ExternalLoginSecretRequest
    {
        /// <summary>
        /// External login authentication method name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string PartyName { get; set; }

        /// <summary>
        /// Secret.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        public string Secret { get; set; }
    }
}
