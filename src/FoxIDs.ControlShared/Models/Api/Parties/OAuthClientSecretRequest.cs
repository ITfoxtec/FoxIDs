using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// OAuth 2.0 client secret request.
    /// </summary>
    public class OAuthClientSecretRequest
    {
        /// <summary>
        /// OAuth 2.0 or OIDC application name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string PartyName { get; set; }

        /// <summary>
        /// Secrets.
        /// </summary>
        [ListLength(1, Constants.Models.OAuthDownParty.Client.SecretsMax, Constants.Models.SecretHash.SecretLength)]
        public List<string> Secrets { get; set; }
    }
}
