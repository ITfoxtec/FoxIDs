using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity.Util;

namespace FoxIDs.SeedTool.Models
{
    public class SeedSettings
    {
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
        /// FoxIDs endpoint.
        /// </summary>
        [Required]
        public string FoxIDsEndpoint { get; set; }
        /// <summary>
        /// Tenant.
        /// </summary>
        [Required]
        public string Tenant { get; set; }
        /// <summary>
        ///  Authority for "tenant/master environment/app-reg".
        /// </summary>
        public string Authority => UrlCombine.Combine(FoxIDsEndpoint, Tenant, "master", ClientId);

        /// <summary>
        /// FoxIDs control endpoint.
        /// </summary>
        [Required]
        public string FoxIDsControlEndpoint { get; set; }

        /// <summary>
        /// FoxIDs control API endpoint.
        /// </summary>
        public string FoxIDsControlApiEndpoint => UrlCombine.Combine(FoxIDsControlEndpoint, "api");
        /// <summary>
        /// FoxIDs tenant API control endpoint.
        /// </summary>
        public string FoxIDsTenantControlApiEndpoint => UrlCombine.Combine(FoxIDsControlApiEndpoint, Tenant);

        /// <summary>
        /// Path to CSV file with users.
        /// </summary>
        [Required]
        public string UsersPath { get; set; }
    }
}
