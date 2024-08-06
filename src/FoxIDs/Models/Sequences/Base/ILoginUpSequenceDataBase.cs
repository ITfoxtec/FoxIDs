using System.Collections.Generic;

namespace FoxIDs.Models.Sequences
{
    public interface ILoginUpSequenceDataBase : IUpSequenceData
    {
        string SessionId { get; set; }
        bool RequireLogoutConsent { get; set; }
        bool PostLogoutRedirect { get; set; }
        bool DoSessionUserRequireLogin { get; set; }
        IEnumerable<string> Acr { get; set; }
        IEnumerable<string> AuthMethods { get; set; }
        TwoFactorAppSequenceStates TwoFactorAppState { get; set; }
        string TwoFactorAppSecret { get; set; }
        string TwoFactorAppNewSecret { get; set; }
        string TwoFactorAppRecoveryCode { get; set; }
    }
}
