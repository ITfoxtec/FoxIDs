using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public  class TrackLogic : LogicBase
    {
        private readonly ITenantRepository tenantRepository;
        private readonly ExternalKeyLogic externalKeyLogic;
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly UpPartyCacheLogic upPartyCacheLogic;

        public TrackLogic(ITenantRepository tenantRepository, ExternalKeyLogic externalKeyLogic, TrackCacheLogic trackCacheLogic, UpPartyCacheLogic upPartyCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.tenantRepository = tenantRepository;
            this.externalKeyLogic = externalKeyLogic;
            this.trackCacheLogic = trackCacheLogic;
            this.upPartyCacheLogic = upPartyCacheLogic;
        }

        public async Task CreateTrackDocumentAsync(Track mTrack, TrackKeyTypes keyType, string tenantName = null, string trackName = null)
        {
            if (keyType == TrackKeyTypes.Contained)
            {
                var certificate = await (RouteBinding.TenantName, mTrack.Name).CreateSelfSignedCertificateBySubjectAsync();
                mTrack.Key = new TrackKey()
                {
                    Type = keyType,
                    Keys = new List<TrackKeyItem> { new TrackKeyItem { Key = await certificate.ToFTJsonWebKeyAsync(true) } }
                };
            }
            else if (keyType == TrackKeyTypes.KeyVaultRenewSelfSigned)
            {
                mTrack.Key = new TrackKey()
                {
                    Type = keyType,
                    ExternalName = await externalKeyLogic.CreateExternalKeyAsync(mTrack, tenantName, trackName)
                };
            }
            else
            {
                throw new NotSupportedException($"Track key type '{keyType}' not supported.");
            }

            await tenantRepository.CreateAsync(mTrack);

            await trackCacheLogic.InvalidateTrackCacheAsync(trackName ?? RouteBinding.TrackName, tenantName ?? RouteBinding.TenantName);
        }

        public async Task CreateLoginDocumentAsync(Track mTrack)
        {
            var mLoginUpParty = new LoginUpParty
            {
                Name = Constants.DefaultLogin.Name,
                EnableCreateUser = true,
                EnableCancelLogin = false,
                SessionLifetime = 36000, // 10 hours
                PersistentSessionLifetimeUnlimited = false,
                LogoutConsent = LoginUpPartyLogoutConsent.IfRequired
            };
            var partyIdKey = new Party.IdKey { TenantName = RouteBinding.TenantName, TrackName = mTrack.Name, PartyName = Constants.DefaultLogin.Name };
            await mLoginUpParty.SetIdAsync(partyIdKey);

            await tenantRepository.CreateAsync(mLoginUpParty);

            await upPartyCacheLogic.InvalidateUpPartyCacheAsync(partyIdKey);
        }
    }
}
