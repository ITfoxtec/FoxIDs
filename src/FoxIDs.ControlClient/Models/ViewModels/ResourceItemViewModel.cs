using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ResourceItemViewModel : ResourceItem
    {
        public ResourceItemViewModel()
        {
            Items = new List<ResourceCultureItem> { new ResourceCultureItem() };
        }

        [Required]
        [Display(Name = "Text look up key (en)")]
        public string Name { get; set; }
    }
}
