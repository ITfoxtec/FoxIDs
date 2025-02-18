using FoxIDs.Models.Logic;

namespace FoxIDs.Models.Sequences
{
    public interface IUpSequenceData : ISequenceData, ILoginRequest
    {
        bool ExternalInitiatedSingleLogout { get; set; }
        string HrdLoginUpPartyName { get; set; }
        string UpPartyId { get; set; }
    }
}
