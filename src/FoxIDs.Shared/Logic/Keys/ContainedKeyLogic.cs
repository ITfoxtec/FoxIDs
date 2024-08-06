using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ContainedKeyLogic : LogicBase
    {
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly TrackCacheLogic trackCacheLogic;

        public ContainedKeyLogic(ITenantDataRepository tenantDataRepository, TrackCacheLogic trackCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.tenantDataRepository = tenantDataRepository;
            this.trackCacheLogic = trackCacheLogic;
        }

        public async Task<Track> RenewCertificateAsync(Track.IdKey idKey, Track track)
        {
            var utcNow = DateTimeOffset.UtcNow;

            if (track.Key.Keys[0].NotAfter < utcNow.AddDays(1).ToUnixTimeSeconds())
            {
                // new key
                track.Key.Keys = new List<TrackKeyItem>
                {
                    await GetNewTrackKeyItemAsync(idKey, track)
                };
                await tenantDataRepository.UpdateAsync(track);
                await trackCacheLogic.InvalidateTrackCacheAsync(idKey);
            }
            else
            {
                if (track.Key.Keys.Count == 1)
                {
                    if (track.Key.Keys[0].NotAfter < utcNow.AddDays(track.KeyAutoRenewDaysBeforeExpiry).ToUnixTimeSeconds())
                    {
                        track.Key.Keys.Add(await GetNewTrackKeyItemAsync(idKey, track));
                        await tenantDataRepository.UpdateAsync(track);
                        await trackCacheLogic.InvalidateTrackCacheAsync(idKey);
                    }
                }
                else if (track.Key.Keys.Count > 1)
                {
                    if (track.Key.Keys[1].NotBefore < utcNow.AddDays(-track.KeyPrimaryAfterDays).ToUnixTimeSeconds())
                    {
                        if (track.Key.Keys[1].NotAfter < utcNow.AddDays(1).ToUnixTimeSeconds())
                        {
                            // new key if only valid for one more day
                            track.Key.Keys = new List<TrackKeyItem>
                            {
                                await GetNewTrackKeyItemAsync(idKey, track)
                            };
                        }
                        else
                        {
                            //swap keys
                            track.Key.Keys = new List<TrackKeyItem> 
                            {
                                track.Key.Keys[1] 
                            };
                        }
                        await tenantDataRepository.UpdateAsync(track);
                        await trackCacheLogic.InvalidateTrackCacheAsync(idKey);
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return track;
        }

        private async Task<TrackKeyItem> GetNewTrackKeyItemAsync(Track.IdKey idKey, Track track)
        {
            var newCertificateItem = await (idKey.TenantName, idKey.TrackName).CreateSelfSignedCertificateBySubjectAsync(track.KeyValidityInMonths);
            return await newCertificateItem.ToTrackKeyItemAsync(true);
        }
    }
}
