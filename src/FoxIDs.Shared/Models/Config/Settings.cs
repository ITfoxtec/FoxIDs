using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class Settings : IValidatableObject
    {
        /// <summary>
        /// FoxIDs endpoint, used in both FoxIDs and FoxIDs Control. Optionally in FoxIDs.
        /// </summary>
        public string FoxIDsEndpoint { get; set; }

        /// <summary>
        /// FoxIDs Control endpoint, used in FoxIDs Control.
        /// </summary>
        public string FoxIDsControlEndpoint { get; set; }

        /// <summary>
        /// Cosmos DB configuration.
        /// </summary>
        [ValidateComplexType]
        public CosmosDbSettings CosmosDb { get; set; }

        /// <summary>
        /// File data configuration.
        /// </summary>
        [ValidateComplexType]
        public FileDataSettings FileData { get; set; } = new FileDataSettings();

        /// <summary>
        /// Key Vault configuration.
        /// </summary>
        [ValidateComplexType]
        public KeyVaultSettings KeyVault { get; set; }

        /// <summary>
        /// Redis Cache configuration.
        /// </summary>
        [ValidateComplexType]
        public RedisCacheSettings RedisCache { get; set; }

        /// <summary>
        /// Cache configuration.
        /// </summary>
        [ValidateComplexType]
        public CacheSettings Cache { get; set; } = new CacheSettings();

        /// <summary>
        /// Only used in development!
        /// The servers client credentials. 
        /// </summary>
        [ValidateComplexType]
        public ClientCredentialSettings ServerClientCredential { get; set; }

        /// <summary>
        /// Specify the selected options.
        /// </summary>
        [Required]
        public OptionsSettings Options { get; set; } = new OptionsSettings();

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Options.DataStorage == DataStorageOptions.CosmosDb)
            {
                if (CosmosDb == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(CosmosDb)} is required if {nameof(Options.DataStorage)} is {Options.DataStorage}.", new[] { nameof(CosmosDb) }));
                }
            }
            if (Options.DataStorage == DataStorageOptions.File)
            {
                if (FileData == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(FileData)} is required if {nameof(Options.DataStorage)} is {Options.DataStorage}.", new[] { nameof(FileData) }));
                }
            }

            if (Options.KeyStorage == KeyStorageOptions.KeyVault)
            {
                if (KeyVault == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(KeyVault)} is required if {nameof(Options.KeyStorage)} is {Options.KeyStorage}.", new[] { nameof(KeyVault) }));
                }
            }

            if (Options.Cache == CacheOptions.Redis)
            {
                if (RedisCache == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(RedisCache)} is required if {nameof(Options.Cache)} is {Options.Cache}.", new[] { nameof(RedisCache) }));
                }
            }
            if (Options.Cache != CacheOptions.None)
            {
                if (Cache == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(Cache)} is required if {nameof(Options.Cache)} is not {CacheOptions.None}.", new[] { nameof(Cache) }));
                }
            }

            return results;
        }
    }
}
