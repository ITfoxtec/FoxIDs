using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class Settings
    {
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
    }
}
