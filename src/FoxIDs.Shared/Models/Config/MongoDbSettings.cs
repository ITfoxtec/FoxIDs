using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class MongoDbSettings
    {
        /// <summary>
        /// Connection string.
        /// </summary>
        [Required]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Default database Name.
        /// </summary>
        [Required]
        public string DatabaseName { get; set; } = "FoxIDs";

        /// <summary>
        /// Tenants Collection Name.
        /// </summary>
        [Required]
        public string TenantsCollectionName { get; set; } = "Tenants";

        /// <summary>
        /// Time-to-live (TTL) Tenants Collection Name.
        /// </summary>
        [Required]
        public string TtlTenantsCollectionName { get; set; } = "TenantsTtl";

        /// <summary>
        /// Cache Collection Name also supporting Time-to-live (TTL).
        /// </summary>
        [Required]
        public string CacheCollectionName { get; set; } = "Cache";
    }
}
