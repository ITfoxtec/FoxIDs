using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class Settings : IValidatableObject
    {
        /// <summary>
        /// FoxIDs endpoint, used in both FoxIDs and FoxIDs Control.
        /// </summary>
        public string FoxIDsEndpoint { get; set; }

        /// <summary>
        /// FoxIDs Control endpoint, used in FoxIDs Control.
        /// </summary>
        public string FoxIDsControlEndpoint { get; set; }

        /// <summary>
        /// Optionally accept to use HTTP.
        /// </summary>
        public bool UseHttp { get; set; }

        [ValidateComplexType]
        public AddressSettings Address { get; set; }

        /// <summary>
        /// Sendgrid configuration.
        /// </summary>
        public SendgridSettings Sendgrid { get; set; }

        /// <summary>
        /// SMTP configuration.
        /// </summary>
        public SmtpSettings Smtp { get; set; }

        /// <summary>
        /// Sms configuration.
        /// </summary>
        [ValidateComplexType]
        public SmsSettings Sms { get; set; }

        /// <summary>
        /// OpenSearch configuration.
        /// </summary>
        [ValidateComplexType]
        public OpenSearchSettings OpenSearch { get; set; }

        /// <summary>
        /// File data configuration.
        /// </summary>
        [ValidateComplexType]
        public FileDataSettings FileData { get; set; }

        /// <summary>
        /// Cosmos DB configuration.
        /// </summary>
        [ValidateComplexType]
        public CosmosDbSettings CosmosDb { get; set; }

        /// <summary>
        /// Mongo DB configuration.
        /// </summary>
        [ValidateComplexType]
        public MongoDbSettings MongoDb { get; set; }

        /// <summary>
        /// PostgreSQL configuration.
        /// </summary>
        [ValidateComplexType]
        public PostgreSqlSettings PostgreSql { get; set; }

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
        [Required]
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

        /// <summary>
        /// Optional proxy secret. Validating the HTTP header "X-FoxIDs-Secret" if not empty.
        /// </summary>
        public string ProxySecret { get; set; }

        /// <summary>
        /// Optional trust proxy scheme header (HTTP/HTTPS). Default false.
        /// </summary>
        public bool TrustProxySchemeHeader { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Options.Log == LogOptions.OpenSearchAndStdoutErrors)
            {
                if (OpenSearch == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(OpenSearch)} is required if {nameof(Options.Log)} is {LogOptions.OpenSearchAndStdoutErrors}.", [nameof(OpenSearch)]));
                }
            }

            if (Options.DataStorage == DataStorageOptions.File || Options.Cache == CacheOptions.File)
            {
                if (FileData == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(FileData)} is required if {nameof(Options.DataStorage)} is {DataStorageOptions.File} or {nameof(Options.Cache)} is {CacheOptions.File}.", [nameof(FileData)]));
                }
            }

            if (Options.DataStorage == DataStorageOptions.CosmosDb)
            {
                if (CosmosDb == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(CosmosDb)} is required if {nameof(Options.DataStorage)} is {DataStorageOptions.CosmosDb}.", [nameof(CosmosDb)]));
                }
            }
            
            if (Options.DataStorage == DataStorageOptions.MongoDb || Options.Cache == CacheOptions.MongoDb)
            {
                if (MongoDb == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(MongoDb)} is required if {nameof(Options.DataStorage)} is {DataStorageOptions.MongoDb} or {nameof(Options.Cache)} is {CacheOptions.MongoDb}.", [nameof(MongoDb)]));
                }
            }
            
            if (Options.DataStorage == DataStorageOptions.PostgreSql || Options.Cache == CacheOptions.PostgreSql)
            {
                if (PostgreSql == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(PostgreSql)} is required if {nameof(Options.DataStorage)} is {DataStorageOptions.PostgreSql} or {nameof(Options.Cache)} is {CacheOptions.PostgreSql}.", [nameof(PostgreSql)]));
                }
            }

            if (Options.KeyStorage == KeyStorageOptions.KeyVault)
            {
                if (KeyVault == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(KeyVault)} is required if {nameof(Options.KeyStorage)} is {KeyStorageOptions.KeyVault}.", [nameof(KeyVault)]));
                }
            }

            if (Options.Cache == CacheOptions.Redis)
            {
                if (RedisCache == null)
                {
                    results.Add(new ValidationResult($"The field {nameof(RedisCache)} is required if {nameof(Options.Cache)} is {CacheOptions.Redis}.", [nameof(RedisCache)]));
                }
            }

            return results;
        }
    }
}
