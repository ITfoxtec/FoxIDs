using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IAllowUpPartyNames
    {
        [Display(Name = "Allow Up-party names")]
        List<string> AllowUpPartyNames { get; set; }
    }
}
