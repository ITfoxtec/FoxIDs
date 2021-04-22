using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    /// <summary>
    /// Only used in development!
    /// </summary>
    public class ClientCredentialSettings
    {
        /// <summary>
        /// Only used in development!
        /// Tenant id.
        /// </summary>
        [Required]
        public string TenantId { get; set; }

        /// <summary>
        /// Only used in development!
        /// Servers client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Only used in development!
        /// Servers client secret
        /// </summary>
        [Required] 
        public string ClientSecret { get; set; }

    }
}
