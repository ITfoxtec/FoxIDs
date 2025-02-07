using FoxIDs.Models.Session;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Logic
{
    public class LoginRequest : ILoginRequest
    {
        public LoginRequest() { }

        public LoginRequest(ILoginRequest loginRequest)
        {
            DownPartyLink = loginRequest.DownPartyLink;
            LoginAction = loginRequest.LoginAction;
            UserId = loginRequest.UserId;
            MaxAge = loginRequest.MaxAge;
            LoginHint = loginRequest.LoginHint;
            Acr = loginRequest.Acr;
        }

        [Required]
        public DownPartySessionLink DownPartyLink { get; set; }

        public LoginAction LoginAction { get; set; }

        public string UserId { get; set; }

        public int? MaxAge { get; set; }

        public string LoginHint { get; set; }

        public IEnumerable<string> Acr { get; set; }
    }
}
