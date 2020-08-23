using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class Resource
    {
        [Length(Constants.Models.Resource.SupportedCulturesMin, Constants.Models.Resource.SupportedCulturesMax, Constants.Models.Resource.SupportedCulturesLength)]
        public List<string> SupportedCultures { get; set; }

        [Length(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        public List<ResourceName> Names { get; set; }

        [Length(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        public List<ResourceItem> Resources { get; set; }
    }
}
