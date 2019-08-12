using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class RedisCacheSettings
    {
        /// <summary>
        /// Redis Cache connection string.
        /// </summary>
        [Required]
        public string ConnectionString { get; set; }
    }
}
