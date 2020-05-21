using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ResourceItem
    {
        [Required]
        public int Id { get; set; }

        [Length(1, 5000)]
        public List<ResourceCultureItem> Items { get; set; }
    }
}
