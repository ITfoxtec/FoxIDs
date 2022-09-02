using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class CacheSettings
    {
        /// <summary>
        /// Time to cache custom domains in seconds (default 24 hours).
        /// </summary>
        [Required]
        public int CustomDomainLifetime { get; set; } = 86400;

        /// <summary>
        /// Time to cache up-parties in seconds (default 6 hours).
        /// </summary>
        [Required] 
        public int UpPartyLifetime { get; set; } = 21600;

        /// <summary>
        /// Time to cache down-parties in seconds (default 6 hours).
        /// </summary>
        [Required]
        public int DownPartyLifetime { get; set; } = 21600;
    }
}
