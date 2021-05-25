using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
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
        [Display(Name = "Service name language")]
        public string Lang { get; set; }

        /// <summary>
        /// Name for the service.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "Service name")]
        public string Name { get; set; }
    }
}
