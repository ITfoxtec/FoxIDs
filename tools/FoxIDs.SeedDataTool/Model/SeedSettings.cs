using UrlCombineLib;

namespace FoxIDs.SeedDataTool.Model
{
    public class SeedSettings
    {
        public string ClientId => DownParty;
        public string ClientSecret { get; set; }

        public string RedirectUri { get; set; }

        public string Authority => FoxIDsEndpoint == null ? null : UrlCombine.Combine(FoxIDsEndpoint, Tenant, Track, DownParty);

        public string FoxIDsEndpoint { get; set; }
        public string Tenant { get; set; }
        public string Track { get; set; }
        public string DownParty { get; set; }

        public string FoxIDsApiEndpoint { get; set; }
        public string FoxIDsMasterApiEndpoint => UrlCombine.Combine(FoxIDsApiEndpoint, "@master");
        public string FoxIDsMasterTrackApiEndpoint => UrlCombine.Combine(FoxIDsApiEndpoint, "master");

        public string PwnedPasswordsSha1OrderedByCountPath { get; set; }
    }
}
