using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models;
using System.Collections.Generic;

namespace FoxIDs.Model
{
    public class ResourceApiModel
    {
        public List<string> SupportedCultures { get; set; }

        public List<ResourceName> Names { get; set; }

        public List<ResourceItem> Resources { get; set; }
    }
}
