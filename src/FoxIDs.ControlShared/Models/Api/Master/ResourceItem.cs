using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ResourceItem
    {
        [Required]
        [Display(Name = "Id")]
        public int Id { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.Resource.ResourcesMin, Constants.Models.Resource.ResourcesMax)]
        [Display(Name = "Texts")]
        public List<ResourceCultureItem> Items { get; set; }
    }
}
