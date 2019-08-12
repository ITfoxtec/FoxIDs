using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class RouteBinding
    {
        public string RouteUrl { get; set; }

        public string Route { get { return $"{TenantName}.{TrackName}.{PartyNameAndBinding}"; } }

        public string TenantName { get; set; }

        public string TrackName { get; set; }

        public string PartyNameAndBinding { get; set; }

        public TrackKey PrimaryKey { get; set; }

        public TrackKey SecondaryKey { get; set; }

        public UpParty UpParty { get; set; }

        public List<PartyDataElement> ToUpParties { get; set; }

        public DownParty DownParty { get; set; }

        public ClaimMappingsDataElement ClaimMappings { get; set; }

        public List<ResourceItem> Resources { get; set; }

        public int SequenceLifetime { get; set; }

        public int PasswordLength { get; set; }

        public bool CheckPasswordComplexity { get; set; }

        public bool CheckPasswordRisk { get; set; }
    }
}
