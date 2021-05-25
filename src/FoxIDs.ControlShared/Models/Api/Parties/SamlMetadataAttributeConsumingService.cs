using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// The AttributeConsumingService element defines a particular service offered by the service
    /// provider in terms of the attributes the service requires or desires.
    /// </summary>
    public class SamlMetadataAttributeConsumingService
    {
        /// <summary>
        /// Language-qualified names for the service.
        /// </summary>
        [ValidateComplexType]
        [Required]
        public SamlMetadataServiceName ServiceName { get; set; }

        /// <summary>
        /// A required element specifying attributes required or desired by this service.
        /// </summary>
        [ValidateComplexType]
        [Length(Constants.Models.SamlParty.MetadataRequestedAttributesMin, Constants.Models.SamlParty.MetadataRequestedAttributesMax)]
        [Display(Name = "Attributes (claims)")]
        public List<SamlMetadataRequestedAttribute> RequestedAttributes { get; set; }
    }
}
