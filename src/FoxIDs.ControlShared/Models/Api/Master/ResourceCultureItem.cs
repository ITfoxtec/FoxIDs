using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ResourceCultureItem
    {
        [Required]
        [MaxLength(Constants.Models.Resource.CultureLength)]
        public string Culture { get; set; }

        [MaxLength(Constants.Models.Resource.ValueLength)]
        public string DefaultValue { get; set; }

        [MaxLength(Constants.Models.Resource.ValueLength)]
        public string Value { get; set; }
    }
}
