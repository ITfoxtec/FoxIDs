using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class Settings
    {
        /// <summary>
        /// FoxIDs control endpoint.
        /// </summary>
        public string FoxIDsControlEndpoint { get; set; }

        /// <summary>
        /// Cosmos DB configuration.
        /// </summary>
        [Required]
        public CosmosDbSettings CosmosDb { get; set; }

        /// <summary>
        /// Key Vault configuration.
        /// </summary>
        [Required]
        public KeyVaultSettings KeyVault { get; set; }

        /// <summary>
        /// Enable master seed if true.
        /// </summary>
        public bool MasterSeedEnabled { get; set; }
    }
}
