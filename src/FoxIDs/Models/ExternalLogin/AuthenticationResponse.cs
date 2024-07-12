using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ExternalLogin

{
    public class AuthenticationResponse
    {
        [Required]
        public IEnumerable<ClaimValue> Claims { get; set; }
    }
}
