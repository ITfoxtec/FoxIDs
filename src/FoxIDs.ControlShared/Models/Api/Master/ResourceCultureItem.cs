using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ResourceCultureItem
    {
        [Required]
        [MaxLength(5)]
        public string Culture { get; set; }

        [Required]
        [MaxLength(500)]
        public string Value { get; set; }
    }
}
