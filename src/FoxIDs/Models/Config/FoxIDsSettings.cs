﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class FoxIDsSettings : Settings
    {
        /// <summary>
        /// FoxIDs redirect to website.
        /// </summary>
        public string WebsiteUrl { get; set; }

        /// <summary>
        /// Redis Cache configuration.
        /// </summary>
        [Required]
        public RedisCacheSettings RedisCache { get; set; }

        /// <summary>
        /// Persistent session max unlimited lifetime in years.
        /// </summary>
        [Required]
        public int PersistentSessionMaxUnlimitedLifetimeYears { get; set; }

        /// <summary>
        /// Cors preflight max age in secunds.
        /// </summary>
        [Required]
        public int CorsPreflightMaxAge { get; set; }

        /// <summary>
        /// Add time before where the token is valid in secunds.
        /// </summary>
        [Required]
        public double SamlTokenAddNotBeforeTime { get; set; }
    }
}
