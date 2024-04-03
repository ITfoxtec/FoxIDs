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
        /// Default Collection Name.
        /// </summary>
        [Required]
        public string CollectionName { get; set; } = "Tenants";
    }
}
