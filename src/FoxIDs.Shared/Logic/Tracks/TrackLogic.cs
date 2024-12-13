using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public  class TrackLogic : LogicBase
    {
        private readonly ITenantDataRepository tenantDataRepository;

        public TrackLogic(ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task CreateTrackDocumentAsync(Track mTrack, string tenantName = null)
        {
            var certificateItem = await (tenantName ?? RouteBinding.TenantName, mTrack.Name).CreateSelfSignedCertificateBySubjectAsync(mTrack.KeyValidityInMonths);
            mTrack.Key = new TrackKey()
            {
                Type = TrackKeyTypes.ContainedRenewSelfSigned,
                Keys = new List<TrackKeyItem> 
                {
                    await certificateItem.ToTrackKeyItemAsync(true)
                }
            };

            await tenantDataRepository.CreateAsync(mTrack);
        }

        public async Task CreateLoginDocumentAsync(Track mTrack)
        {
            var mLoginUpParty = new LoginUpParty
            {
                DisplayName = "Default",
                Name = Constants.DefaultLogin.Name,
                EnableCreateUser = true,
                EnableCancelLogin = false,
                SessionLifetime = 36000, // 10 hours
                PersistentSessionLifetimeUnlimited = false,
                LogoutConsent = LoginUpPartyLogoutConsents.IfRequired
            };
            var partyIdKey = new Party.IdKey { TenantName = RouteBinding.TenantName, TrackName = mTrack.Name, PartyName = Constants.DefaultLogin.Name };
            await mLoginUpParty.SetIdAsync(partyIdKey);

            await tenantDataRepository.CreateAsync(mLoginUpParty);
        }
    }
}
