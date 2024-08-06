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
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly UpPartyCacheLogic upPartyCacheLogic;

        public TrackLogic(ITenantDataRepository tenantDataRepository, TrackCacheLogic trackCacheLogic, UpPartyCacheLogic upPartyCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.tenantDataRepository = tenantDataRepository;
            this.trackCacheLogic = trackCacheLogic;
            this.upPartyCacheLogic = upPartyCacheLogic;
        }

        public async Task CreateTrackDocumentAsync(Track mTrack, string tenantName = null, string trackName = null)
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

            await trackCacheLogic.InvalidateTrackCacheAsync(trackName ?? RouteBinding.TrackName, tenantName ?? RouteBinding.TenantName);
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

            await upPartyCacheLogic.InvalidateUpPartyCacheAsync(partyIdKey);
        }
    }
}
