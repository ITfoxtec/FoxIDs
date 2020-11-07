using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IAllowUpPartyNames
    {
        [Display(Name = "Allow Up Party names")]
        List<string> AllowUpPartyNames { get; set; }
    }
}
