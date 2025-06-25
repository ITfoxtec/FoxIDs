using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class AuthenticationRequest
    {
        [Required]
        public ExternalLoginUsernameTypes UsernameType { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        public string Username { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        public string Password { get; set; }
    }
}
