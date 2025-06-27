using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ResourceCulture
    {
        [Required]
        [MaxLength(Constants.Models.Resource.CultureLength)]
        public string Culture { get; set; }
    }
}
