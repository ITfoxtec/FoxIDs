using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UserControlProfileRequest : UserControlProfile
    {
        [Required]
        [MaxLength(Constants.Models.Claim.ValueLength)]
        [Display(Name = "User sub")]
        public string UserSub { get; set; }    
    }
}
