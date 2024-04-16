using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class CosmosDbSettings
    {
        /// <summary>
        /// Cosmos DB endpoint.
        /// </summary>
        [Required]
        public string EndpointUri { get; set; }
        /// <summary>
        /// Cosmos DB primary key.
        /// </summary>
        [Required]
        public string PrimaryKey { get; set; }
        /// <summary>
        /// Default database Id.
        /// </summary>
        [Required]
        public string DatabaseId { get; set; } = "FoxIDs";
        /// <summary>
        /// Default Collection Id.
        /// </summary>
        [Required]
        public string ContainerId { get; set; } = "Tenants";
        /// <summary>
        /// Time-to-live (TTL) Collection Id.
        /// </summary>
        [Required]
        public string TtlContainerId { get; set; } = "Tenants";
    }
}
