using FoxIDs.Models.Api;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcUpPartyProfileViewModel : UpPartyProfileViewModel
    {
        [Required]
        public OidcUpClientProfileViewModel Client { get; set; } = new OidcUpClientProfileViewModel();
    }
}
