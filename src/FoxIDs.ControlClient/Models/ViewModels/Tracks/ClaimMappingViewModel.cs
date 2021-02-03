using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class ClaimMappingViewModel
    {
        [Display(Name = "Mappings between JWT and SAML claim types")]
        public List<ClaimMap> ClaimMappings { get; set; }
    }
}
