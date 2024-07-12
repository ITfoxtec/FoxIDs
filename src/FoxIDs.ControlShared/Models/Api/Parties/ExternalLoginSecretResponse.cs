using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// External login secret response.
    /// </summary>
    public class ExternalLoginSecretResponse
    {
        /// <summary>
        /// Secret info.
        /// </summary>
        [MaxLength(Constants.Models.SecretHash.InfoLength)]
        public string Info { get; set; }
    }
}
