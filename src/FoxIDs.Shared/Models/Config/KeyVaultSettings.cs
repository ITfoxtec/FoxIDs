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
    }
}
