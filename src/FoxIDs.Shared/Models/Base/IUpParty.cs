using System;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface IUpParty : IParty
    {
        [Obsolete($"Use {nameof(Issuers)} instead.")]
        string Issuer { set; }
        List<string> Issuers { get; set; }
        string SpIssuer { get; set; }
        PartyBindingPatterns PartyBindingPattern { get; set; }
        int SessionLifetime { get; set; }
        int SessionAbsoluteLifetime { get; set; }
        bool PersistentSessionLifetimeUnlimited { get; set; }
        int PersistentSessionAbsoluteLifetime { get; set; }
        bool DisableSingleLogout { get; set; }
        List<string> HrdIPAddressesAndRanges { get; set; }
        List<string> HrdDomains { get; set; }
        List<string> HrdRegularExpressions { get; set; }
        bool HrdAlwaysShowButton { get; set; }
        [Obsolete($"Use {nameof(HrdAlwaysShowButton)} instead.")]
        bool? HrdShowButtonWithDomain { get; set; }
        string HrdDisplayName { get; set; }
        string HrdLogoUrl { get; set; }
        bool DisableUserAuthenticationTrust { get; set; }
        bool DisableTokenExchangeTrust { get; set; }
    }
}