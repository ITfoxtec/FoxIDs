using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class CreateUserRequest : UserBase
    {
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
