using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity.Util;

namespace FoxIDs.MasterSeedTool.Models
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
        /// Seed tool master tenant.
        /// </summary>
        [Required]
        public string MasterTenant { get; set; }
        /// <summary>
        /// Seed tool master environment.
        /// </summary>
        [Required]
        public string MasterTrack { get; set; }
        /// <summary>
        /// FoxIDs tenant/environment/app-reg authority.
        /// </summary>
        public string Authority => UrlCombine.Combine(FoxIDsEndpoint, MasterTenant, MasterTrack, ClientId);

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
        /// FoxIDs master API control endpoint.
        /// </summary>
        public string FoxIDsMasterControlApiEndpoint => UrlCombine.Combine(FoxIDsControlApiEndpoint, "@master");

        /// <summary>
        /// Pwned passwords (SHA1 ordered by count) path.
        /// </summary>
        [Required]
        public string PwnedPasswordsPath { get; set; }
    }
}
