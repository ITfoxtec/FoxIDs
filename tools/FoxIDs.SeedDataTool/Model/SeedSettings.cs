using FoxIDs.Models.Config;
using System.ComponentModel.DataAnnotations;
using UrlCombineLib;

namespace FoxIDs.SeedDataTool.Model
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
        /// Seed tool redirect uri.
        /// </summary>
        [Required]
        public string RedirectUri { get; set; }

        public string Authority => UrlCombine.Combine(FoxIDsEndpoint, MasterTenant, MasterTrack, DownParty);

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
        /// Seed tool down party (client id).
        /// </summary>
        [Required]
        public string DownParty { get; set; }

        /// <summary>
        /// FoxIDs api endpoint.
        /// </summary>
        [Required]
        public string FoxIDsApiEndpoint { get; set; }
        /// <summary>
        /// FoxIDs master api endpoint.
        /// </summary>
        public string FoxIDsMasterApiEndpoint => UrlCombine.Combine(FoxIDsApiEndpoint, "@master");
        /// <summary>
        /// FoxIDs master track api endpoint.
        /// </summary>
        public string FoxIDsMasterTrackApiEndpoint => UrlCombine.Combine(FoxIDsApiEndpoint, "master");

        /// <summary>
        /// Pwned passwords (SHA1 ordered by count) path.
        /// </summary>
        [Required]
        public string PwnedPasswordsPath { get; set; }

        /// <summary>
        /// Cosmos DB configuration.
        /// </summary>
        public CosmosDbSettings CosmosDb { get; set; }
    }
}
