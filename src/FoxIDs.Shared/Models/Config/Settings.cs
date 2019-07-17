namespace FoxIDs.Models.Config
{
    public class Settings
    {
        /// <summary>
        /// Cosmos DB configuration.
        /// </summary>
        public CosmosDbSettings CosmosDb { get; set; }

        /// <summary>
        /// Key Vault configuration.
        /// </summary>
        public KeyVaultSettings KeyVault { get; set; }
    }
}
