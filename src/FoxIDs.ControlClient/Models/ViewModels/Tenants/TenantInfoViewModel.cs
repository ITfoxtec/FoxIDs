using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TenantInfoViewModel
    {
        [Display(Name = "Tenant name")]
        public string Name { get; set; }

        public string LoginUri { get; set; }
    }
}
