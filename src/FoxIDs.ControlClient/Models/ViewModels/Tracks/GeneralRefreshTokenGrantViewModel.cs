using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralRefreshTokenGrantViewModel : RefreshTokenGrant
    {
        public GeneralRefreshTokenGrantViewModel()
        { }

        public GeneralRefreshTokenGrantViewModel(RefreshTokenGrant refreshTokenGrant)
        {
            RefreshToken = refreshTokenGrant.RefreshToken;
            Email = refreshTokenGrant.Email;
            Phone = refreshTokenGrant.Phone;
            Username = refreshTokenGrant.Username;
            ClientId = refreshTokenGrant.ClientId;
            UpPartyName = refreshTokenGrant.UpPartyName;
            UpPartyType = refreshTokenGrant.UpPartyType;
        }

        public string Error { get; set; }

        public RefreshTokenGrantViewModel Details { get; set; }
    }
}
