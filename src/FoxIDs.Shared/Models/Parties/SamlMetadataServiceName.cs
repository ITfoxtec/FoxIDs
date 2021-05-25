using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    /// <summary>
    /// Language-qualified name for the service.
    /// </summary>
    public class SamlMetadataServiceName
    {
        /// <summary>
        /// Language.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SamlParty.Up.MetadataServiceNameLangLength)]
        [JsonProperty(PropertyName = "lang")]
        public string Lang { get; set; }

        /// <summary>
        /// Name for the service.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
