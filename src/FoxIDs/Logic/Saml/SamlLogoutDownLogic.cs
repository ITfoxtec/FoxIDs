﻿using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using Saml2Http = ITfoxtec.Identity.Saml2.Http;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using FoxIDs.Models.Logic;
using Microsoft.IdentityModel.Tokens.Saml2;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.Session;
using FoxIDs.Logic.Tracks;

namespace FoxIDs.Logic
{
    public class SamlLogoutDownLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly HrdLogic hrdLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly Saml2ConfigurationLogic saml2ConfigurationLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly SamlClaimsDownLogic samlClaimsDownLogic;
        private readonly ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;

        public SamlLogoutDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, HrdLogic hrdLogic, SecurityHeaderLogic securityHeaderLogic, Saml2ConfigurationLogic saml2ConfigurationLogic, ClaimTransformLogic claimTransformLogic, SamlClaimsDownLogic samlClaimsDownLogic, ClaimsOAuthDownLogic<OidcDownClient, OidcDownScope, OidcDownClaim> claimsOAuthDownLogic, SingleLogoutDownLogic singleLogoutDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.hrdLogic = hrdLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.saml2ConfigurationLogic = saml2ConfigurationLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.samlClaimsDownLogic = samlClaimsDownLogic;
            this.claimsOAuthDownLogic = claimsOAuthDownLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
        }

        public async Task<IActionResult> LogoutRequestAsync(string partyId, Saml2Http.HttpRequest samlHttpRequest)
        {
            logger.ScopeTrace(() => "AppReg, SAML Logout request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<SamlDownParty>(partyId);
            ValidatePartyLogoutSupport(party);
            await sequenceLogic.SetDownPartyAsync(partyId, PartyTypes.Saml2);

            if (samlHttpRequest.Binding is Saml2RedirectBinding || samlHttpRequest.Binding is Saml2PostBinding)
            {
                logger.ScopeTrace(() => $"Binding, configured '{party.LogoutBinding.RequestBinding}', actual '{samlHttpRequest.Binding.GetType().Name}'");
                return await LogoutRequestAsync(party, samlHttpRequest);
            }
            else
            {
                throw new NotSupportedException($"Binding '{samlHttpRequest.Binding.GetType().Name}' not supported.");
            }
        }

        private void ValidatePartyLogoutSupport(SamlDownParty party)
        {
            if (party.LogoutBinding == null || party.LoggedOutUrl.IsNullOrEmpty() || party.Keys?.Count <= 0)
            {
                throw new EndpointException("Logout not configured.") { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> LogoutRequestAsync(SamlDownParty party, Saml2Http.HttpRequest samlHttpRequest)
        {
            var samlConfig = await saml2ConfigurationLogic.GetSamlDownConfigAsync(party);

            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig);
            samlHttpRequest.Binding.ReadSamlRequest(samlHttpRequest, saml2LogoutRequest);
            logger.ScopeTrace(() => $"SAML Logout request '{saml2LogoutRequest.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);

            try
            {
                ValidateLogoutRequest(party, saml2LogoutRequest);

                try
                {
                    samlHttpRequest.Binding.Unbind(samlHttpRequest, saml2LogoutRequest);
                    logger.ScopeTrace(() => "AppReg, SAML Logout request accepted.", triggerEvent: true);
                }
                catch (Exception ex)
                {
                    var isex = saml2ConfigurationLogic.GetInvalidSignatureValidationCertificateException(samlConfig, ex);
                    if (isex != null)
                    {
                        throw isex;
                    }
                    throw;
                }

                await sequenceLogic.SaveSequenceDataAsync(new SamlDownSequenceData
                {
                    Id = saml2LogoutRequest.Id.Value,
                    RelayState = samlHttpRequest.Binding.RelayState
                });

                var toUpParty = await hrdLogic.GetUpPartyAndDeleteHrdSelectionAsync();
                logger.ScopeTrace(() => $"Request, Authentication type '{toUpParty.Type}'.");
                switch (toUpParty.Type)
                {
                    case PartyTypes.Login:
                        return await serviceProvider.GetService<LogoutUpLogic>().LogoutRedirect(toUpParty, GetLogoutRequest(party, saml2LogoutRequest));
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcRpInitiatedLogoutUpLogic<OidcUpParty, OidcUpClient>>().EndSessionRequestRedirectAsync(toUpParty, GetLogoutRequest(party, saml2LogoutRequest));
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutUpLogic>().LogoutRequestRedirectAsync(toUpParty, GetSamlLogoutRequest(party, saml2LogoutRequest));
                    case PartyTypes.TrackLink:
                        return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutUpLogic>().LogoutRequestRedirectAsync(toUpParty, GetLogoutRequest(party, saml2LogoutRequest));
                    case PartyTypes.ExternalLogin:
                        return await serviceProvider.GetService<ExternalLogoutUpLogic>().LogoutRedirect(toUpParty, GetLogoutRequest(party, saml2LogoutRequest));

                    default:
                        throw new NotSupportedException($"Connection type '{toUpParty.Type}' not supported.");
                }
            }
            catch (SamlRequestException ex)
            {
                logger.Error(ex);
                return await LogoutResponseAsync(party, samlConfig, saml2LogoutRequest.Id.Value, samlHttpRequest.Binding.RelayState, ex.Status);
            }
        }

        private LogoutRequest GetLogoutRequest(SamlDownParty party, Saml2LogoutRequest saml2LogoutRequest)
        {
            return new LogoutRequest
            {
                DownPartyLink = new DownPartySessionLink { SupportSingleLogout = !string.IsNullOrWhiteSpace(party.SingleLogoutUrl), Id = party.Id, Type = party.Type },
                SessionId = saml2LogoutRequest.SessionIndex,
                RequireLogoutConsent = false,
                PostLogoutRedirect = true,
            };
        }

        private LogoutRequest GetSamlLogoutRequest(SamlDownParty party, Saml2LogoutRequest saml2LogoutRequest)
        {
            return new LogoutRequest
            {
                DownPartyLink = new DownPartySessionLink { SupportSingleLogout = !string.IsNullOrWhiteSpace(party.SingleLogoutUrl), Id = party.Id, Type = party.Type },
                SessionId = saml2LogoutRequest.SessionIndex,
                RequireLogoutConsent = false,
                PostLogoutRedirect = true
            };
        }

        private void ValidateLogoutRequest(SamlDownParty party, Saml2LogoutRequest saml2LogoutRequest)
        {
            var requestIssuer = saml2LogoutRequest.Issuer;
            logger.SetScopeProperty(Constants.Logs.Issuer, requestIssuer);

            if (!party.Issuer.Equals(requestIssuer))
            {
                throw new SamlRequestException($"Invalid issuer '{requestIssuer}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
            }
        }

        public async Task<IActionResult> LogoutResponseAsync(string partyId, Saml2StatusCodes status = Saml2StatusCodes.Success, string sessionIndex = null)
        {
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);

            var party = await tenantDataRepository.GetAsync<SamlDownParty>(partyId);
            ValidatePartyLogoutSupport(party);

            var samlConfig = await saml2ConfigurationLogic.GetSamlDownConfigAsync(party, includeSigningCertificate: true);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<SamlDownSequenceData>(false);
            return await LogoutResponseAsync(party, samlConfig, sequenceData.Id, sequenceData.RelayState, status, sessionIndex);
        }

        private Task<IActionResult> LogoutResponseAsync(SamlDownParty party, Saml2Configuration samlConfig, string inResponseTo, string relayState, Saml2StatusCodes status, string sessionIndex = null) 
        {
            logger.ScopeTrace(() => $"AppReg, SAML Logout response{(status != Saml2StatusCodes.Success ? " error" : string.Empty)}, Status code '{status}'.");

            var binding = party.LogoutBinding.ResponseBinding;
            logger.ScopeTrace(() => $"Binding '{binding}'");
            switch (binding)
            {
                case SamlBindingTypes.Redirect:
                    return LogoutResponseAsync(party, samlConfig, inResponseTo, relayState, new Saml2RedirectBinding(), status, sessionIndex);
                case SamlBindingTypes.Post:
                    return LogoutResponseAsync(party, samlConfig, inResponseTo, relayState, new Saml2PostBinding(), status, sessionIndex);
                default:
                    throw new NotSupportedException($"SAML binding '{binding}' not supported.");
            }
        }

        private async Task<IActionResult> LogoutResponseAsync(SamlDownParty party, Saml2Configuration samlConfig, string inResponseTo, string relayState, Saml2Binding binding, Saml2StatusCodes status, string sessionIndex)
        {
            binding.RelayState = relayState;

            var saml2LogoutResponse = new Saml2LogoutResponse(samlConfig)
            {
                InResponseTo = new Saml2Id(inResponseTo),
                Status = status,
                Destination = new Uri(party.LoggedOutUrl),
                SessionIndex = sessionIndex
            };

            binding.Bind(saml2LogoutResponse);
            logger.ScopeTrace(() => $"SAML Logout response '{saml2LogoutResponse.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
            logger.ScopeTrace(() => $"Logged out URL '{party.LoggedOutUrl}'.");
            logger.ScopeTrace(() => "AppReg, SAML Logout response.", triggerEvent: true);

            await sequenceLogic.RemoveSequenceDataAsync<SamlDownSequenceData>();
            if (party.RestrictFormAction)
            {
                securityHeaderLogic.AddFormAction(party.LoggedOutUrl);
            }
            else
            {
                securityHeaderLogic.AddFormActionAllowAll();
            }
            if (binding is Saml2RedirectBinding saml2RedirectBinding)
            {
                return await saml2RedirectBinding.ToActionFormResultAsync();
            }
            if (binding is Saml2PostBinding saml2PostBinding)
            {
                return await saml2PostBinding.ToActionFormResultAsync();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public async Task<IActionResult> SingleLogoutRequestAsync(string partyId, SingleLogoutSequenceData sequenceData)
        {
            logger.ScopeTrace(() => "AppReg, SAML Single Logout request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<SamlDownParty>(partyId);
            if (!ValidatePartySingleLogoutSupport(party))
            {
                return await singleLogoutDownLogic.HandleSingleLogoutAsync(sequenceData);
            }
            
            var claims = await claimsOAuthDownLogic.FromJwtToSamlClaimsAsync(sequenceData.Claims.ToClaimList());

            switch (party.LogoutBinding.RequestBinding)
            {
                case SamlBindingTypes.Redirect:
                    return await SingleLogoutRequestAsync(party, new Saml2RedirectBinding(), claims);
                case SamlBindingTypes.Post:
                    return await SingleLogoutRequestAsync(party, new Saml2PostBinding(), claims);
                default:
                    throw new NotSupportedException($"SAML binding '{party.LogoutBinding.RequestBinding}' not supported.");
            }
        }

        private bool ValidatePartySingleLogoutSupport(SamlDownParty party)
        {
            if (party.LogoutBinding == null || party.SingleLogoutUrl.IsNullOrEmpty() || party.Keys?.Count <= 0)
            {
                return false;
            }
            return true;
        }

        private async Task<IActionResult> SingleLogoutRequestAsync(SamlDownParty party, Saml2Binding binding, IEnumerable<Claim> claims)
        {
            var samlConfig = await saml2ConfigurationLogic.GetSamlDownConfigAsync(party, includeSigningCertificate: true);

            claims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);

            var saml2LogoutRequest = new Saml2LogoutRequest(samlConfig)
            {
                NameId = samlClaimsDownLogic.GetNameId(claims),
                Destination = new Uri(party.SingleLogoutUrl),
                SessionIndex = samlClaimsDownLogic.GetSessionIndex(claims)
            };

            binding.RelayState = await sequenceLogic.CreateExternalSequenceIdAsync();
            binding.Bind(saml2LogoutRequest);
            logger.ScopeTrace(() => $"SAML Single Logout request '{saml2LogoutRequest.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
            logger.ScopeTrace(() => $"Single logged out URL '{party.SingleLogoutUrl}'.");
            logger.ScopeTrace(() => "AppReg, SAML Single Logout request.", triggerEvent: true);

            if (party.RestrictFormAction)
            {
                securityHeaderLogic.AddFormAction(party.SingleLogoutUrl);
            }
            else
            {
                securityHeaderLogic.AddFormActionAllowAll();
            }
            if (binding is Saml2RedirectBinding saml2RedirectBinding)
            {
                return await saml2RedirectBinding.ToActionFormResultAsync();
            }
            if (binding is Saml2PostBinding saml2PostBinding)
            {
                return await saml2PostBinding.ToActionFormResultAsync();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public async Task<IActionResult> SingleLogoutResponseAsync(string partyId, Saml2Http.HttpRequest samlHttpRequest)
        {
            logger.ScopeTrace(() => "AppReg, SAML Single Logout response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<SamlDownParty>(partyId);

            if (samlHttpRequest.Binding is Saml2RedirectBinding || samlHttpRequest.Binding is Saml2PostBinding)
            {
                logger.ScopeTrace(() => $"Binding, configured '{party.LogoutBinding.ResponseBinding}', actual '{samlHttpRequest.Binding.GetType().Name}'");
                return await SingleLogoutResponseAsync(party, samlHttpRequest);
            }
            else
            {
                throw new NotSupportedException($"Binding '{samlHttpRequest.Binding.GetType().Name}' not supported.");
            }
        }

        private async Task<IActionResult> SingleLogoutResponseAsync(SamlDownParty party, Saml2Http.HttpRequest samlHttpRequest)
        {
            var samlConfig = await saml2ConfigurationLogic.GetSamlDownConfigAsync(party);

            var saml2LogoutResponse = new Saml2LogoutResponse(samlConfig);
            samlHttpRequest.Binding.ReadSamlResponse(samlHttpRequest, saml2LogoutResponse);
            logger.ScopeTrace(() => $"SAML Single Logout response '{saml2LogoutResponse.XmlDocument.OuterXml}'.", traceType: TraceTypes.Message);
            
            ValidateLogoutResponse(party, saml2LogoutResponse);
            await sequenceLogic.ValidateExternalSequenceIdAsync(samlHttpRequest.Binding.RelayState);

            try
            {
                samlHttpRequest.Binding.Unbind(samlHttpRequest, saml2LogoutResponse);
                logger.ScopeTrace(() => "AppReg, SAML Single Logout response accepted.", triggerEvent: true);

            }
            catch (Exception ex)
            {
                var isex = saml2ConfigurationLogic.GetInvalidSignatureValidationCertificateException(samlConfig, ex);
                if (isex != null)
                {
                    throw isex;
                }
                throw;
            }

            return await singleLogoutDownLogic.HandleSingleLogoutAsync();
        }

        private void ValidateLogoutResponse(SamlDownParty party, Saml2LogoutResponse saml2LogoutResponse)
        {
            var requestIssuer = saml2LogoutResponse.Issuer;
            logger.SetScopeProperty(Constants.Logs.Issuer, requestIssuer);

            if (!party.Issuer.Equals(requestIssuer))
            {
                throw new SamlRequestException($"Invalid issuer '{requestIssuer}'.") { RouteBinding = RouteBinding, Status = Saml2StatusCodes.Responder };
            }
        }
    }
}
