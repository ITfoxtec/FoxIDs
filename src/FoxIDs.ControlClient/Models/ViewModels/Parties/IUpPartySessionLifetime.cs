using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IUpPartySessionLifetime
    {
        [Display(Name = "Session lifetime in seconds (active session if greater than 0)")]
        int SessionLifetime { get; set; }

        [Display(Name = "Session absolute lifetime in seconds (active if greater than 0)")]
        int SessionAbsoluteLifetime { get; set; }

        [Display(Name = "Persistent session absolute lifetime in seconds (active if greater than 0)")]
        int PersistentSessionAbsoluteLifetime { get; set; } 

        [Display(Name = "Persistent session lifetime unlimited")]
        bool PersistentSessionLifetimeUnlimited { get; set; }
    }
}
