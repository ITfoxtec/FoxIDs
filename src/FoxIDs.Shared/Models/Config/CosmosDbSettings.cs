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
        public string DatabaseId { get; set; }
        /// <summary>
        /// Default Collection Id.
        /// </summary>
        [Required]
        public string CollectionId { get; set; }
        /// <summary>
        /// Time-to-live (TTL) Collection Id.
        /// </summary>
        [Required]
        public string TtlCollectionId { get; set; }
    }
}
