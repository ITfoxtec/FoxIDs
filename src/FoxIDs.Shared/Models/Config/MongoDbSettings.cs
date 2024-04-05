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
        /// Cache Collection Name.
        /// </summary>
        [Required]
        public string CacheCollectionName { get; set; } = "Cache";

        /// <summary>
        /// Time-to-live (TTL) Cache Collection Name.
        /// </summary>
        [Required]
        public string TtlCacheCollectionName { get; set; } = "CacheTtl";
    }
}
