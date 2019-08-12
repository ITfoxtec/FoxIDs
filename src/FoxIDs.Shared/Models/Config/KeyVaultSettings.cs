using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class KeyVaultSettings
    {
        /// <summary>
        /// Key vault endpoint.
        /// </summary>
        [Required]
        public string EndpointUri { get; set; }
        /// <summary>
        /// Only used in development!
        /// Key vault client id
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Only used in development!
        /// Key vault client secret
        /// </summary>
        public string ClientSecret { get; set; }
    }
}
