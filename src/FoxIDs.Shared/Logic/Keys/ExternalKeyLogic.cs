using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using System.Security.Cryptography;
using RSAKeyVaultProvider;
using ITfoxtec.Identity.Models;
using FoxIDs.Infrastructure;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using FoxIDs.Repository;
using FoxIDs.Logic.Caches.Providers;
using Azure.Security.KeyVault.Secrets;

namespace FoxIDs.Logic
{
    public class ExternalKeyLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly ICacheProvider cacheProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly TrackCacheLogic trackCacheLogic;
        private readonly TokenCredential tokenCredential;

        public ExternalKeyLogic(Settings settings, ICacheProvider cacheProvider, ITenantDataRepository tenantDataRepository, TrackCacheLogic trackCacheLogic, TokenCredential tokenCredential, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.cacheProvider = cacheProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.trackCacheLogic = trackCacheLogic;
            this.tokenCredential = tokenCredential;
        }

        public async Task DeleteExternalKeyAsync(string externalName)
        {
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            await certificateClient.StartDeleteCertificateAsync(externalName);
        }

        public RSA GetExternalRSAKey(RouteTrackKey trackKey, RouteTrackKeyItem keyItem)
        {
            return GetExternalRSAKey(trackKey.ExternalName, keyItem.ExternalId, keyItem.Key);
        }

        private RSA GetExternalRSAKey(string externalName, string externalId, JsonWebKey publicKey)
        {
            return RSAFactory.Create(tokenCredential, new Uri(UrlCombine.Combine(settings.KeyVault.EndpointUri, "keys", externalName, externalId)), new Azure.Security.KeyVault.Keys.JsonWebKey(publicKey.ToRsa()));
        }

        public async Task<JsonWebKey> GetExternalKeyAsync(ClientKey clientKey)
        {
            var secretClient = new SecretClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);
            KeyVaultSecret keyVaultSecret = secretClient.GetSecret(clientKey.ExternalName);
            var certificate = new X509Certificate2(Convert.FromBase64String(keyVaultSecret.Value), string.Empty, keyStorageFlags: X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            if (certificate == null)
            {
                throw new Exception($"External client key certificate '{clientKey.ExternalName}' from Key Vault is null.");
            }
            return await certificate.ToFTJsonWebKeyAsync(true);
        }

        public async Task<TrackKeyExternal> GetTrackKeyItemsAsync(TelemetryScopedLogger scopedLogger, string tenantName, string trackName, Track track)
        {
            var key = CacheTrackKeyExternalKey(tenantName, trackName, track.Key.ExternalName);

            var trackKeyExternalValue = await cacheProvider.GetAsync(key);
            if (!trackKeyExternalValue.IsNullOrEmpty())
            {
                return trackKeyExternalValue.ToObject<TrackKeyExternal>();
            }

            var trackKeyExternal = await LoadTrackKeyExternalFromKeyVaultAsync(scopedLogger, track);
            if (trackKeyExternal != null)
            {
                await cacheProvider.SetAsync(key, trackKeyExternal.ToJson(), track.KeyExternalCacheLifetime);
            }
            return trackKeyExternal;
        }

        private string CacheTrackKeyExternalKey(string tenantName, string trackName, string name)
        {
            return $"track_key_ext_{tenantName}_{trackName}_{name}";
        }

        private async Task<TrackKeyExternal> LoadTrackKeyExternalFromKeyVaultAsync(TelemetryScopedLogger scopedLogger, Track track)
        {
            var now = DateTimeOffset.UtcNow;
            var certificateClient = new CertificateClient(new Uri(settings.KeyVault.EndpointUri), tokenCredential);

            var externalKeys = new List<(string Id, DateTimeOffset? NotBefore)>();
            var certificateVersions = certificateClient.GetPropertiesOfCertificateVersionsAsync(track.Key.ExternalName);
            await foreach (var certificateVersion in certificateVersions)
            {
                if (certificateVersion.Enabled == true && certificateVersion.ExpiresOn >= now)
                {
                    externalKeys.Add((certificateVersion.Version, certificateVersion.NotBefore));
                }
            }

            if (externalKeys.Count <= 0)
            {
                try
                {
                    throw new Exception($"Track key external certificate '{track.Key.ExternalName}' do not exist in Key Vault, probably because it is not ready in Key Vault.");
                }
                catch (Exception ex)
                {
                    scopedLogger.Warning(ex);
                    return null;
                }
            }

            var trackKeyExternal = new TrackKeyExternal();
            if (externalKeys.Count == 1)
            {
                trackKeyExternal.Keys = new List<TrackKeyExternalItem> { new TrackKeyExternalItem { ExternalId = externalKeys.First().Id } };
            }
            else
            {
                var topTwoExternalKeys = externalKeys.OrderByDescending(e => e.NotBefore).Take(2);
                var firstExternalKey = topTwoExternalKeys.First();
                if (firstExternalKey.NotBefore <= now.AddDays(-track.KeyPrimaryAfterDays))
                {
                    trackKeyExternal.Keys = new List<TrackKeyExternalItem> { new TrackKeyExternalItem { ExternalId = firstExternalKey.Id }, new TrackKeyExternalItem { ExternalId = topTwoExternalKeys.Last().Id } };
                }
                else
                {
                    trackKeyExternal.Keys = new List<TrackKeyExternalItem> { new TrackKeyExternalItem { ExternalId = topTwoExternalKeys.Last().Id }, new TrackKeyExternalItem { ExternalId = firstExternalKey.Id } };
                }
            }

            foreach (var keyItem in trackKeyExternal.Keys)
            {
                var certificateWithPolicy = certificateClient.GetCertificateVersion(track.Key.ExternalName, keyItem.ExternalId);
                var certificateRawValue = certificateWithPolicy?.Value?.Cer;
                if (certificateRawValue == null)
                {
                    throw new Exception($"Track key external certificate '{track.Key.ExternalName}' version '{keyItem.ExternalId}' from Key Vault is null.");
                }
                keyItem.Key = await new X509Certificate2(certificateRawValue).ToFTJsonWebKeyAsync();
            }

            return trackKeyExternal;
        }

        public async Task<Track> PhasedOutExternalKeyAsync(TelemetryScopedLogger scopedLogger, Track.IdKey idKey, Track track)
        {
            if(!track.Key.ExternalName.IsNullOrWhiteSpace())
            {
                var trackKeyExternal = await GetTrackKeyItemsAsync(scopedLogger, idKey.TenantName, idKey.TrackName, track);
                if (trackKeyExternal == null)
                {
                    // new key and type
                    track.Key = new TrackKey()
                    {
                        Type = TrackKeyTypes.ContainedRenewSelfSigned,
                        Keys = new List<TrackKeyItem>
                        {
                            await GetNewTrackKeyItemAsync(idKey, track)
                        }
                    };
                    await tenantDataRepository.UpdateAsync(track);
                    await trackCacheLogic.InvalidateTrackCacheAsync(idKey);
                }
                else if (track.Key.Keys == null)
                {
                    if (trackKeyExternal.Keys.Count == 1)
                    {
                        var newCertificateItem = await (idKey.TenantName, idKey.TrackName).CreateSelfSignedCertificateBySubjectAsync(track.KeyValidityInMonths);
                        track.Key.Keys = new List<TrackKeyItem>
                        {
                            await newCertificateItem.ToTrackKeyItemAsync(true)
                        };
                        await tenantDataRepository.UpdateAsync(track);
                        await trackCacheLogic.InvalidateTrackCacheAsync(idKey);
                    }                  
                }
                else
                {
                    var utcNow = DateTimeOffset.UtcNow;
                    if (track.Key.Keys[0].NotBefore < utcNow.AddDays(-(track.KeyPrimaryAfterDays - 2)).ToUnixTimeSeconds())
                    {
                        var externalName = track.Key.ExternalName;
                        if (track.Key.Keys[0].NotAfter < utcNow.AddDays(1).ToUnixTimeSeconds())
                        {
                            // new key and type if only valid for one more day
                            track.Key = new TrackKey()
                            {
                                Type = TrackKeyTypes.ContainedRenewSelfSigned,
                                Keys = new List<TrackKeyItem>
                                {
                                    await GetNewTrackKeyItemAsync(idKey, track)
                                }
                            };
                        }
                        else
                        {
                            // change container type
                            track.Key.Type = TrackKeyTypes.ContainedRenewSelfSigned;
                            track.Key.ExternalName = null;
                        }
                        await tenantDataRepository.UpdateAsync(track);
                        await trackCacheLogic.InvalidateTrackCacheAsync(idKey);

                        await DeleteExternalKeyAsync(externalName);
                    }
                }
            }

            return track;
        }

        private async Task<TrackKeyItem> GetNewTrackKeyItemAsync(Track.IdKey idKey, Track track)
        {
            var newCertificateItem = await (idKey.TenantName, idKey.TrackName).CreateSelfSignedCertificateBySubjectAsync(track.KeyValidityInMonths);
            return await newCertificateItem.ToTrackKeyItemAsync(true);
        }

        public async Task PhasedOutExternalClientKeyAsync<TParty, TClient>(TParty party) where TParty : OidcUpParty<TClient> where TClient : OidcUpClient
        {
            foreach (var clientKey in party.Client.ClientKeys) 
            {
                if(clientKey.Type == ClientKeyTypes.KeyVaultImport)
                {
                    clientKey.Type = ClientKeyTypes.Contained;
                    clientKey.Key = await GetExternalKeyAsync(clientKey);
                    clientKey.ExternalId = null;

                    await tenantDataRepository.UpdateAsync(party);
                    await DeleteExternalKeyAsync(clientKey.ExternalName);
                }
            }
        }
    }
}
