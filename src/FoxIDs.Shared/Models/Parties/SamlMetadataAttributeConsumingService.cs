using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
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
        [Required]
        [JsonProperty(PropertyName = "service_name")]
        public SamlMetadataServiceName ServiceName { get; set; }

        /// <summary>
        /// A required element specifying attributes required or desired by this service.
        /// </summary>
        [ListLength(Constants.Models.SamlParty.MetadataRequestedAttributesMin, Constants.Models.SamlParty.MetadataRequestedAttributesMax)]
        [JsonProperty(PropertyName = "requested_attributes")]
        public List<SamlMetadataRequestedAttribute> RequestedAttributes { get; set; }
    }
}
