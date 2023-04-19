using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.Session;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkAuthDownLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ClaimsDownLogic claimsDownLogic;

        public TrackLinkAuthDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, ClaimTransformLogic claimTransformLogic, ClaimsDownLogic claimsDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.claimsDownLogic = claimsDownLogic;
        }

        public async Task<IActionResult> AuthRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "Down, Track link auth request.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TrackLinkDownParty>(partyId);

            var keySequenceString = HttpContext.Request.Query[Constants.Routes.KeySequenceKey];
            var keySequence = await sequenceLogic.ValidateSequenceAsync(keySequenceString, trackName: party.ToUpTrackName);
            var keySequenceData = await sequenceLogic.ValidateKeySequenceDataAsync<TrackLinkUpSequenceData>(keySequence, party.ToUpTrackName, remove: false);
            if (party.ToUpPartyName != keySequenceData.KeyName)
            {
                throw new Exception($"Incorrect up-party name '{keySequenceData.KeyName}', expected up-party name '{party.ToUpPartyName}'.");
            }

            await sequenceLogic.SaveSequenceDataAsync(new TrackLinkDownSequenceData { KeyName = party.Name, UpPartySequenceString = keySequenceString });

            var toUpParties = RouteBinding.ToUpParties;
            if (toUpParties.Count() == 1)
            {
                var toUpParty = toUpParties.First();
                logger.ScopeTrace(() => $"Request, Up type '{toUpParty:Type}'.");
                switch (toUpParty.Type)
                {
                    case PartyTypes.Login:
                        return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(toUpParty, GetLoginRequest(party, keySequenceData));
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestRedirectAsync(toUpParty, GetLoginRequest(party, keySequenceData));
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestRedirectAsync(toUpParty, GetLoginRequest(party, keySequenceData));
                    case PartyTypes.TrackLink:
                        return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthRequestAsync(toUpParty, GetLoginRequest(party, keySequenceData));

                    default:
                        throw new NotSupportedException($"Party type '{toUpParty.Type}' not supported.");
                }
            }
            else
            {
                return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(GetLoginRequest(party, keySequenceData));
            }
        }

        private LoginRequest GetLoginRequest(TrackLinkDownParty party, TrackLinkUpSequenceData keySequenceData)
        {
            return new LoginRequest
            {
                DownPartyLink = new DownPartySessionLink { SupportSingleLogout = true, Id = party.Id, Type = party.Type },
                LoginAction = keySequenceData.LoginAction,
                UserId = keySequenceData.UserId,
                MaxAge = keySequenceData.MaxAge,
                EmailHint = keySequenceData.LoginEmailHint,
                Acr = keySequenceData.Acr
            };
        }

        public async Task<IActionResult> AuthResponseAsync(string partyId, List<Claim> claims, string error = null, string errorDescription = null)
        {
            logger.ScopeTrace(() => "Down, Track link auth response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantRepository.GetAsync<TrackLinkDownParty>(partyId);

            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkDownSequenceData>(remove: false);

            if (error.IsNullOrEmpty())
            {
                logger.ScopeTrace(() => $"Down, Track link received JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                claims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                logger.ScopeTrace(() => $"Down, Track link output / Up, Track link received - JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

                claims = await claimsDownLogic.FilterJwtClaimsAsync(claimsDownLogic.GetFilterClaimTypes(party.Claims), claims);

                var clientClaims = claimsDownLogic.GetClientJwtClaims(party.Claims);
                if (clientClaims?.Count() > 0)
                {
                    claims.AddRange(clientClaims);
                }
            }

            sequenceData.Claims = claims?.ToClaimAndValues();
            sequenceData.Error = error;
            sequenceData.ErrorDescription = errorDescription;
            await sequenceLogic.SaveSequenceDataAsync(sequenceData, setKeyValidUntil: true);

            return HttpContext.GetTrackUpPartyUrl(party.ToUpTrackName, party.ToUpPartyName, Constants.Routes.TrackLinkController, Constants.Endpoints.TrackLinkAuthResponse, includeKeySequence: true).ToRedirectResult();
        }
    }
}
