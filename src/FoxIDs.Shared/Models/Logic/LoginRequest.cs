using FoxIDs.Models.Session;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Logic
{
    public class LoginRequest
    {
        [Required]
        public DownPartySessionLink DownPartyLink { get; set; }

        public LoginAction LoginAction { get; set; }

        public string UserId { get; set; }

        public int? MaxAge { get; set; }

        public string EmailHint { get; set; }

        public IEnumerable<string> Acr { get; set; }
    }
}
