using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity.Util;

namespace FoxIDs.MasterSeedTool.Models
{
    public class SeedSettings
    {
        /// <summary>
        /// FoxIDs control endpoint.
        /// </summary>
        [Required]
        public string FoxIDsControlEndpoint { get; set; }

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
        /// FoxIDs master API control endpoint.
        /// </summary>
        public string FoxIDsMasterControlApiEndpoint => UrlCombine.Combine(FoxIDsControlEndpoint, "api", "@master");

        /// <summary>
        /// Pwned passwords (SHA1 ordered by count) path.
        /// </summary>
        [Required]
        public string PwnedPasswordsPath { get; set; }
    }
}
