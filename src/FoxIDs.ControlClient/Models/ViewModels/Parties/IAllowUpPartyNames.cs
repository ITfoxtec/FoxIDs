using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IAllowUpPartyNames
    {
        [Display(Name = "Allowed authentication methods")]
        List<UpPartyLink> AllowUpParties { get; set; }
    }
}
