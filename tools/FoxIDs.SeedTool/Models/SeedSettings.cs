using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity.Util;

namespace FoxIDs.SeedTool.Models
{
    public class SeedSettings
    {
        /// <summary>
        /// FoxIDs control endpoint.
        /// </summary>
        [Required]
        public string FoxIDsControlEndpoint { get; set; }

        [Required]
        public string Tenant { get; set; }

        [Required]
        public string Environment { get; set; }

        /// <summary>
        /// Seed tool authority.
        /// </summary>
        [Required]
        public string Authority { get; set; }

        /// <summary>
        /// Seed tool client id.
        /// </summary>
        [Required]
        public string ClientId { get; set; }

        /// <summary>
        /// Seed tool client secret.
        /// </summary>
        [Required]
        public string ClientSecret { get; set; }

        /// <summary>
        /// Seed tool scope.
        /// </summary>
        [Required]
        public string Scope { get; set; }

        /// <summary>
        /// FoxIDs API control endpoint.
        /// </summary>
        public string FoxIDsControlApiEndpoint => UrlCombine.Combine(FoxIDsControlEndpoint, "api", Tenant, Environment);

        /// <summary>
        /// Path to CSV file with users.
        /// </summary>
        [Required]
        public string UsersSvcPath { get; set; }
    }
}
