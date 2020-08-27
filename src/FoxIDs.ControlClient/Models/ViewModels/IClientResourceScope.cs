using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IClientResourceScope
    {
        [Display(Name = "Resource and scopes")]
        List<OAuthDownResourceScope> ResourceScopes { get; set; }

        bool DefaultResourceScope { get; set; }

        [Display(Name = "Scopes")]
        List<string> DefaultResourceScopeScopes { get; set; }
    }
}
