using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcDownPartyViewModel
    {
        [Display(Name = "Up Party name")]
        public string Name { get; set; }
    }
}
