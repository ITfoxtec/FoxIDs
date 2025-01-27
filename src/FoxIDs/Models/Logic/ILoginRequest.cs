using FoxIDs.Models.Session;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Logic
{
    public interface ILoginRequest
    {
        [Required]
        DownPartySessionLink DownPartyLink { get; set; }

        LoginAction LoginAction { get; set; }

        string UserId { get; set; }

        int? MaxAge { get; set; }

        string LoginHint { get; set; }

        IEnumerable<string> Acr { get; set; }
    }
}
