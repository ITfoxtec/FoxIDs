using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralRefreshTokenGrantViewModel : RefreshTokenGrantViewModel
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
            CreateTime = refreshTokenGrant.CreateTime;
            TimeToLive = refreshTokenGrant.TimeToLive;
        }

        public string Error { get; set; }

        public RefreshTokenGrantViewModel Details { get; set; }
    }
}
