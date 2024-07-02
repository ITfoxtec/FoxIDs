using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.Session;
using System.Collections.Generic;

namespace FoxIDs.Models.Sequences
{
    public interface IUpSequenceData : ISequenceData
    {
        bool ExternalInitiatedSingleLogout { get; set; }
        DownPartySessionLink DownPartyLink { get; set; }
        IEnumerable<ClaimAndValues> SessionClaims { get; set; }
        List<DownPartySessionLink> SessionDownPartyLinks { get; set; }
        string HrdLoginUpPartyName { get; set; }
        string UpPartyId { get; set; }
        LoginAction LoginAction { get; set; }
        string UserId { get; set; }
        int? MaxAge { get; set; }
        string LoginEmailHint { get; set; }
    }
}
