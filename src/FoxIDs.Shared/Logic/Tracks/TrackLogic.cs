using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public  class TrackLogic : LogicBase
    {
        const string loginName = "login";
        private readonly ITenantRepository tenantRepository;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TrackLogic(ITenantRepository tenantRepository, ExternalKeyLogic externalKeyLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.tenantRepository = tenantRepository;
            this.externalKeyLogic = externalKeyLogic;
        }

        public async Task CreateTrackDocumentAsync(Track mTrack, string tenantName = null)
        {
            mTrack.Key = new TrackKey()
            {
                Type = TrackKeyType.KeyVaultRenewSelfSigned,
                ExternalName = await externalKeyLogic.CreateExternalKeyAsync(mTrack, tenantName)
            };

            await tenantRepository.CreateAsync(mTrack);
        }

        public async Task CreateLoginDocumentAsync(Track mTrack)
        {
            var mLoginUpParty = new LoginUpParty
            {
                Name = loginName,
                EnableCreateUser = true,
                EnableCancelLogin = false,
                SessionLifetime = 36000, // 10 hours
                PersistentSessionLifetimeUnlimited = false,
                LogoutConsent = LoginUpPartyLogoutConsent.IfRequired
            };
            await mLoginUpParty.SetIdAsync(new Party.IdKey { TenantName = RouteBinding.TenantName, TrackName = mTrack.Name, PartyName = loginName });

            await tenantRepository.CreateAsync(mLoginUpParty);
        }
    }
}
