using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ExtendedUiRequest
    {
        [Required]
        public IEnumerable<ElementValue> Elements { get; set; }

        [Required]
        public IEnumerable<ClaimValue> Claims { get; set; }
    }
}
