using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SamlMetadataReadUpLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly ITenantRepository tenantRepository;

        public SamlMetadataReadUpLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IConnectionMultiplexer redisConnectionMultiplexer, ITenantRepository tenantRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tenantRepository = tenantRepository;
        }

        public async Task CheckMetadataAndUpdateUpPartyAsync(SamlUpParty party)
        {
            if (party.UpdateState != PartyUpdateStates.Automatic)
            {
                return;
            }

            var lastUpdated = DateTimeOffset.FromUnixTimeSeconds(party.LastUpdated);
            if (lastUpdated.AddSeconds(party.MetadataUpdateRate.Value) >= DateTimeOffset.UtcNow)
            {
                return;
            }

            var db = redisConnectionMultiplexer.GetDatabase();
            var key = UpdateUpPartyWaitPeriodKey(party.Id);
            if (await db.KeyExistsAsync(key))
            {
                logger.ScopeTrace(() => $"Up party '{party.Id}' not updated with SAML 2.0 metadata because another update is in progress.");
                return;
            }
            else
            {
                await db.StringSetAsync(key, true, TimeSpan.FromSeconds(settings.UpPartyUpdateWaitPeriod));
            }

            var failingUpdateCount = (long?)await db.StringGetAsync(FailingUpdateUpPartyCountKey(party.Id));
            if (failingUpdateCount.HasValue && failingUpdateCount.Value >= settings.UpPartyMaxFailingUpdate)
            {
                party.UpdateState = PartyUpdateStates.AutomaticStopped;
                await tenantRepository.SaveAsync(party);
                await db.KeyDeleteAsync(FailingUpdateUpPartyCountKey(party.Id));
                return;
            }

            try
            {
                try
                {
                    var entityDescriptor = new EntityDescriptor();
                    entityDescriptor.ReadIdPSsoDescriptorFromUrl(new Uri(party.MetadataUrl));
                    if (entityDescriptor.IdPSsoDescriptor != null)
                    {
                        party.Issuer = entityDescriptor.EntityId;
                        var singleSignOnServices = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.FirstOrDefault();
                        if (singleSignOnServices == null)
                        {
                            throw new Exception("IdPSsoDescriptor SingleSignOnServices is empty.");
                        }

                        party.AuthnUrl = singleSignOnServices.Location?.OriginalString;
                        party.AuthnBinding.RequestBinding = GetSamlBindingTypes(singleSignOnServices.Binding?.OriginalString);

                        var singleLogoutDestination = GetSingleLogoutServices(entityDescriptor.IdPSsoDescriptor.SingleLogoutServices);
                        if (singleLogoutDestination != null)
                        {
                            party.LogoutUrl = singleLogoutDestination.Location?.OriginalString;
                            var singleLogoutResponseLocation = singleLogoutDestination.ResponseLocation?.OriginalString;
                            if (!string.IsNullOrEmpty(singleLogoutResponseLocation))
                            {
                                party.SingleLogoutResponseUrl = singleLogoutResponseLocation;
                            }
                            if (party.LogoutBinding == null)
                            {
                                party.LogoutBinding = new SamlBinding { ResponseBinding = SamlBindingTypes.Post };
                            }
                            party.LogoutBinding.RequestBinding = GetSamlBindingTypes(singleLogoutDestination.Binding?.OriginalString);
                        }

                        if (entityDescriptor.IdPSsoDescriptor.SigningCertificates?.Count() > 0)
                        {
                            party.Keys = await Task.FromResult(entityDescriptor.IdPSsoDescriptor.SigningCertificates.Select(c => c.ToFTJsonWebKey()).ToList());
                        }
                        else
                        {
                            party.Keys = null;
                        }

                        if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
                        {
                            party.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
                        }
                    }
                    else
                    {
                        throw new Exception("IdPSsoDescriptor not loaded from metadata.");
                    }
                }
                catch (Exception ex)
                {
                    throw new EndpointException("Failed to read SAML 2.0 metadata.", ex) { RouteBinding = RouteBinding };
                }

                await tenantRepository.SaveAsync(party);
                logger.ScopeTrace(() => $"Up party '{party.Id}' updated by SAML 2.0 metadata.", triggerEvent: true);

                await db.KeyDeleteAsync(FailingUpdateUpPartyCountKey(party.Id));
            }
            catch (Exception ex)
            {
                await db.StringIncrementAsync(FailingUpdateUpPartyCountKey(party.Id));
                logger.Warning(ex);
            }
        }

        private SingleLogoutService GetSingleLogoutServices(IEnumerable<SingleLogoutService> singleLogoutServices)
        {
            var singleLogoutService = singleLogoutServices.Where(s => s.Binding.OriginalString == ProtocolBindings.HttpPost.OriginalString).FirstOrDefault();
            if (singleLogoutService != null)
            {
                return singleLogoutService;
            }
            else
            {
                return singleLogoutServices.FirstOrDefault();
            }
        }

        private SamlBindingTypes GetSamlBindingTypes(string binding)
        {
            if(binding == ProtocolBindings.HttpPost.OriginalString)
            {
                return SamlBindingTypes.Post;
            }
            else if (binding == ProtocolBindings.HttpRedirect.OriginalString)
            {
                return SamlBindingTypes.Redirect;
            }
            else
            {
                throw new Exception($"Binding '{binding}' not supported.");
            }
        }

        private string UpdateUpPartyWaitPeriodKey(string partyId)
        {
            return $"update_up_party_wait_period_{RouteBinding.TenantNameDotTrackName}_{partyId}";
        }

        private string FailingUpdateUpPartyCountKey(string partyId)
        {
            return $"failing_up_party_update_count_{RouteBinding.TenantNameDotTrackName}_{partyId}";
        }
    }
}
