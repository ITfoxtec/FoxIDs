namespace FoxIDs.Models.Config
{
    public class CosmosDbSettings
    {
        public string EndpointUri { get; set; }
        public string PrimaryKey { get; set; }
        public string DatabaseId { get; set; }
        /// <summary>
        /// Default Collection Id.
        /// </summary>
        public string CollectionId { get; set; }
        /// <summary>
        /// Time-to-live (TTL) Collection Id.
        /// </summary>
        public string TtlCollectionId { get; set; }
    }
}
