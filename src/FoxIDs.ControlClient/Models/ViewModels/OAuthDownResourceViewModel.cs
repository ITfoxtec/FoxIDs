using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OAuthDownResourceViewModel
    {
        public OAuthDownResourceViewModel()
        {
            Scopes = new List<string>();
        }

        public bool DefaultScope { get; set; } = true;

        [Length(0, Constants.Models.OAuthDownParty.Resource.ScopesMax, Constants.Models.OAuthDownParty.ScopesLength)]
        [Display(Name = "Scopes")]
        public List<string> Scopes { get; set; }
    }
}
