using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Logic
{
    public class LoginRequest
    {
        [Required]
        public Party DownParty { get; set; }

        public LoginAction LoginAction { get; set; }

        public string UserId { get; set; }

        public int? MaxAge { get; set; }

        public string EmailHint { get; set; }
    }
}
