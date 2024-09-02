using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface IUpParty : IParty
    {
        string Issuer { set; }
        List<string> Issuers { get; set; }
        string SpIssuer { get; set; }
        PartyBindingPatterns PartyBindingPattern { get; set; }
        int SessionLifetime { get; set; }
        int SessionAbsoluteLifetime { get; set; }
        bool PersistentSessionLifetimeUnlimited { get; set; }
        int PersistentSessionAbsoluteLifetime { get; set; }
        bool DisableSingleLogout { get; set; }
        List<string> HrdDomains { get; set; }
        bool HrdShowButtonWithDomain { get; set; }
        string HrdDisplayName { get; set; }
        string HrdLogoUrl { get; set; }
        bool DisableUserAuthenticationTrust { get; set; }
        bool DisableTokenExchangeTrust { get; set; }
        public List<UpPartyProfile> Profiles { get; set; }
    }
}