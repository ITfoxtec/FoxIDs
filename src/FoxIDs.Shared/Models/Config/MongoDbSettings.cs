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
        /// Master Collection Name.
        /// </summary>
        [Required]
        public string MasterCollectionName { get; set; } = "Master";

        /// <summary>
        /// Master Time-to-live (TTL) Collection Name.
        /// </summary>
        [Required]
        public string MasterTtlCollectionName { get; set; } = "MasterTtl";

        /// <summary>
        /// Tenants Collection Name.
        /// </summary>
        [Required]
        public string TenantsCollectionName { get; set; } = "Tenants";

        /// <summary>
        /// Tenants Time-to-live (TTL) Collection Name.
        /// </summary>
        [Required]
        public string TenantsTtlCollectionName { get; set; } = "TenantsTtl";

        /// <summary>
        /// Cache Collection Name also supporting Time-to-live (TTL).
        /// </summary>
        [Required]
        public string CacheCollectionName { get; set; } = "Cache";
    }
}
