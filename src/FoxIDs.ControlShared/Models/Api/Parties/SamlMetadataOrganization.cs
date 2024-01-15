using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// The Organization element specifies basic contact information about the company or organization that is publishing the metadata document.
    /// The use of this element is always optional. Its content is informative in
    /// nature and does not directly map to any core SAML elements or attributes.
    /// </summary>
    public class SamlMetadataOrganization
    {
        /// <summary>
        /// Specifies the name of the organization responsible for the SAML entity or role.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Organization name")]
        public string OrganizationName { get; set; }

        /// <summary>
        /// Specifies the display name of the organization.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Organization display name")]
        public string OrganizationDisplayName { get; set; }

        /// <summary>
        /// Specifies the URL of the organization.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Organization URL")]
        public string OrganizationUrl { get; set; }
    }
}
