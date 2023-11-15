using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface ISamlMetadataAttributeConsumingServiceVievModel
    {
        [ListLength(Constants.Models.SamlParty.MetadataAttributeConsumingServicesMin, Constants.Models.SamlParty.MetadataAttributeConsumingServicesMax)]
        [Display(Name = "Optional attribute consuming services in metadata")]
        public List<SamlMetadataAttributeConsumingService> MetadataAttributeConsumingServices { get; set; }
    }
}
