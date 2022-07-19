using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class Settings
    {
        /// <summary>
        /// Optionally set host endpoint and overwrite the actually hosting domain.
        /// </summary>
        public string HostEndpoint { get; set; }

        /// <summary>
        /// FoxIDs Control endpoint, used in FoxIDs Control.
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
        /// Redis Cache configuration.
        /// </summary>
        [Required]
        public RedisCacheSettings RedisCache { get; set; }

        /// <summary>
        /// Cache configuration.
        /// </summary>
        public CacheSettings Cache { get; set; } = new CacheSettings();

        /// <summary>
        /// Enable master seed if true.
        /// </summary>
        public bool MasterSeedEnabled { get; set; }

        /// <summary>
        /// Optional proxy secret. Validating the HTTP header "X-FoxIDs-Secret" if not empty.
        /// </summary>
        public string ProxySecret { get; set; }

        /// <summary>
        /// Only used in development!
        /// The servers client credentials. 
        /// </summary>
        public ClientCredentialSettings ServerClientCredential { get; set; }
    }
}
