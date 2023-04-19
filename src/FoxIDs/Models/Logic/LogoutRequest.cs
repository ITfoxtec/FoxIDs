using FoxIDs.Models.Session;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Logic
{
    public class LogoutRequest
    {
        [Required]
        public DownPartySessionLink DownPartyLink { get; set; }

        public string SessionId { get; set; }
         
        public bool RequireLogoutConsent { get; set; }

        public bool PostLogoutRedirect { get; set; }        
    }
}
