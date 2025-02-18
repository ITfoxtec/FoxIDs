using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ClaimsResponse
    {
        [Required]
        public IEnumerable<ClaimValue> Claims { get; set; }
    }
}
