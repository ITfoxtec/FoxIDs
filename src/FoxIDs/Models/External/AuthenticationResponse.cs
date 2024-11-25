using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class AuthenticationResponse
    {
        [Required]
        public IEnumerable<ClaimValue> Claims { get; set; }
    }
}
