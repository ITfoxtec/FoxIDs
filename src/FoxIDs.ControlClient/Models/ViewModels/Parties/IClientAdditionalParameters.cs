using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IClientAdditionalParameters
    {
        [Display(Name = "Additional parameters")]
        public List<OAuthAdditionalParameter> AdditionalParameters { get; set; }
    }
}
