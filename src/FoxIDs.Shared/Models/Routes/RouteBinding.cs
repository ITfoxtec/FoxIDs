using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class RouteBinding
    {
        public bool HasCustomDomain { get; set; }

        public string TenantName { get; set; }

        public string TrackName { get; set; }

        public string RouteUrl { get; set; }

        public string TenantNameDotTrackName { get { return $"{TenantName}.{TrackName}"; } }
 
        public string Route { get { return $"{TenantName}.{TrackName}.{PartyNameAndBinding}"; } }

        public string PartyNameAndBinding { get; set; }

        public RouteTrackKey Key { get; set; }

        public UpParty UpParty { get; set; }

        public List<UpPartyLink> ToUpParties { get; set; }
        public bool DefaultToUpParties { get; set; }

        public DownParty DownParty { get; set; }

        public List<ClaimMap> ClaimMappings { get; set; }

        public List<ResourceItem> Resources { get; set; }

        public int SequenceLifetime { get; set; }

        public int MaxFailingLogins { get; set; }

        public int FailingLoginCountLifetime { get; set; }

        public int FailingLoginObservationPeriod { get; set; }

        public int PasswordLength { get; set; }

        public bool CheckPasswordComplexity { get; set; }

        public bool CheckPasswordRisk { get; set; }

        public List<string> AllowIframeOnDomains { get; set; }

        public SendEmail SendEmail { get; set; }

        public Logging Logging { get; set; }
    }
}
