using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class CacheSettings
    {
        /// <summary>
        /// Time to plans in seconds (default 24 hours).
        /// </summary>
        [Required]
        public int PlanLifetime { get; set; } = 86400;

        /// <summary>
        /// Time to cache tenants in seconds (default 24 hours).
        /// </summary>
        [Required]
        public int TenantLifetime { get; set; } = 86400;

        /// <summary>
        /// Time to cache tracks in seconds (default 24 hours).
        /// </summary>
        [Required]
        public int TrackLifetime { get; set; } = 86400;

        /// <summary>
        /// Time to cache authentication methods in seconds (default 6 hours).
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
