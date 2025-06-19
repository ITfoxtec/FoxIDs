using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ExtendedUiResponse
    {
        [Required]
        public IEnumerable<ClaimValue> Claims { get; set; }
    }
}
