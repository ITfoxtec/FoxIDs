using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity.Util;

namespace FoxIDs.SeedTool.Model
{
    public class SeedSettings
    {
        /// <summary>
        /// Seed tool client id.
        /// </summary>
        public string ClientId => DownParty;
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
        /// Seed tool master track.
        /// </summary>
        [Required]
        public string MasterTrack { get; set; }
        /// <summary>
        /// Seed tool down-party (client id).
        /// </summary>
        [Required]
        public string DownParty { get; set; }
        /// <summary>
        /// FoxIDs tenant/track/down-party authority.
        /// </summary>
        public string Authority => UrlCombine.Combine(FoxIDsEndpoint, MasterTenant, MasterTrack, DownParty);

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
