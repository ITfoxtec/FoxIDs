using FoxIDs.Models.Logic;
using FoxIDs.Models.Session;
using System.Collections.Generic;

namespace FoxIDs.Models.Sequences
{
    public interface IUpSequenceData : ISequenceData, ILoginRequest
    {
        bool ExternalInitiatedSingleLogout { get; set; }
        IEnumerable<ClaimAndValues> SessionClaims { get; set; }
        List<DownPartySessionLink> SessionDownPartyLinks { get; set; }
        string HrdLoginUpPartyName { get; set; }
        string UpPartyId { get; set; }
    }
}
