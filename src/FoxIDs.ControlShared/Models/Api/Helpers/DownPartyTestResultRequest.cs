using ITfoxtec.Identity;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class DownPartyTestResultRequest
    {
        [Required]
        [MaxLength(IdentityConstants.MessageLength.StateMax)]
        [Display(Name = "State")]
        public string State { get; set; }

        [Required]
        [MaxLength(IdentityConstants.MessageLength.NonceMax)]
        [Display(Name = "Nonce")]
        public string Nonce { get; set; }

        [Required]
        [MaxLength(IdentityConstants.MessageLength.CodeMax)]
        [Display(Name = "Code")]
        public string Code { get; set; }
    }
}
