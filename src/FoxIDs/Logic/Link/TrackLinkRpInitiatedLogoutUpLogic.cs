using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TrackLinkRpInitiatedLogoutUpLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SessionUpPartyLogic sessionUpPartyLogic;
        private readonly SingleLogoutLogic singleLogoutLogic;

        public TrackLinkRpInitiatedLogoutUpLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, SessionUpPartyLogic sessionUpPartyLogic, SingleLogoutLogic singleLogoutLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.sessionUpPartyLogic = sessionUpPartyLogic;
            this.singleLogoutLogic = singleLogoutLogic;
        }

        public async Task<IActionResult> LogoutRequestRedirectAsync(UpPartyLink partyLink, LogoutRequest logoutRequest, bool isSingleLogout = false)
        {
            logger.ScopeTrace(() => "AuthMethod, Environment Link RP initiated logout request redirect.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            await logoutRequest.ValidateObjectAsync();

            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(partyId);

            await sequenceLogic.SaveSequenceDataAsync(new TrackLinkUpSequenceData
            {
                IsSingleLogout = isSingleLogout,
                KeyName = partyLink.Name,
                DownPartyLink = logoutRequest?.DownPartyLink,
                UpPartyId = partyId,
                UpPartyProfileName = partyLink.ProfileName,
                SessionId = logoutRequest?.SessionId,
                RequireLogoutConsent = logoutRequest?.RequireLogoutConsent ?? false
            });

            return HttpContext.GetUpPartyUrl(partyLink.Name, Constants.Routes.TrackLinkController, Constants.Endpoints.UpJump.TrackLinkRpLogoutRequestJump, includeSequence: true, partyBindingPattern: party.PartyBindingPattern).ToRedirectResult();
        }

        public async Task<IActionResult> LogoutRequestAsync(string partyId)
        {
            logger.ScopeTrace(() => "AuthMethod, Environment Link RP initiated logout request.");
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkUpSequenceData>(remove: false);
            if (!sequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }
            logger.SetScopeProperty(Constants.Logs.UpPartyId, sequenceData.UpPartyId);

            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(sequenceData.UpPartyId);

            var session = await sessionUpPartyLogic.GetSessionAsync(party);
            if (session == null)
            {
                if (sequenceData.IsSingleLogout)
                {
                    return await singleLogoutLogic.HandleSingleLogoutUpAsync();
                }
                else
                {
                    return await SingleLogoutDoneAsync(party.Id);
                }                
            }
            else
            {
                _ = await sessionUpPartyLogic.DeleteSessionAsync(party, session);
                sequenceData.SessionId = session.ExternalSessionId;
            }

            await sequenceLogic.SaveSequenceDataAsync(sequenceData, setKeyValidUntil: true);

            var profile = GetProfile(party, sequenceData);

            var selectedUpParties = party.SelectedUpParties;
            if (profile != null && profile.SelectedUpParties?.Count() > 0)
            {
                selectedUpParties = profile.SelectedUpParties;
            }

            return HttpContext.GetTrackDownPartyUrl(party.ToDownTrackName, party.ToDownPartyName, selectedUpParties, Constants.Routes.TrackLinkController, Constants.Endpoints.TrackLinkRpLogoutRequest, includeKeySequence: true).ToRedirectResult();
        }

        private TrackLinkUpPartyProfile GetProfile(TrackLinkUpParty party, TrackLinkUpSequenceData trackLinkUpSequenceData)
        {
            if (!trackLinkUpSequenceData.UpPartyProfileName.IsNullOrEmpty() && party.Profiles != null)
            {
                return party.Profiles.Where(p => p.Name == trackLinkUpSequenceData.UpPartyProfileName).FirstOrDefault();
            }
            return null;
        }

        public async Task<IActionResult> SingleLogoutDoneAsync(string partyId)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkUpSequenceData>(remove: true);
            if (!sequenceData.IsSingleLogout && !sequenceData.UpPartyId.Equals(partyId, StringComparison.Ordinal))
            {
                throw new Exception("Invalid authentication method id.");
            }
            return await LogoutResponseDownAsync(sequenceData);
        }

        public async Task<IActionResult> LogoutResponseAsync(string partyId)
        {
            logger.ScopeTrace(() => "AppReg, Environment Link RP initiated logout response.");
            logger.SetScopeProperty(Constants.Logs.DownPartyId, partyId);
            var party = await tenantDataRepository.GetAsync<TrackLinkUpParty>(partyId);

            var keySequenceString = HttpContext.Request.Query[Constants.Routes.KeySequenceKey];
            var keySequence = await sequenceLogic.ValidateSequenceAsync(keySequenceString, trackName: party.ToDownTrackName);
            var keySequenceData = await sequenceLogic.ValidateKeySequenceDataAsync<TrackLinkDownSequenceData>(keySequence, party.ToDownTrackName);
            if (party.ToDownPartyName != keySequenceData.KeyName)
            {
                throw new Exception($"Incorrect application registration name '{keySequenceData.KeyName}', expected application registration name '{party.ToDownPartyName}'.");
            }

            await sequenceLogic.ValidateAndSetSequenceAsync(keySequenceData.UpPartySequenceString);
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<TrackLinkUpSequenceData>(remove: party.DisableSingleLogout);

            if (sequenceData.IsSingleLogout)
            {
                return await singleLogoutLogic.HandleSingleLogoutUpAsync();
            }
            else
            {
                if (party.DisableSingleLogout)
                {
                    await sessionUpPartyLogic.DeleteSessionTrackCookieGroupAsync(party);
                    return await LogoutResponseDownAsync(sequenceData);
                }
                else
                {
                    (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutLogic.InitializeSingleLogoutAsync(party, sequenceData.DownPartyLink, sequenceData);
                    if (doSingleLogout)
                    {
                        return await singleLogoutLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                    }
                    else
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>();
                        return await LogoutResponseDownAsync(sequenceData);
                    }
                }
            }
        }

        public async Task<IActionResult> LogoutResponseDownAsync(TrackLinkUpSequenceData sequenceData)
        {
            try
            {
                logger.ScopeTrace(() => $"Response, Application type {sequenceData.DownPartyLink.Type}.");
                switch (sequenceData.DownPartyLink.Type)
                {
                    case PartyTypes.OAuth2:
                        throw new NotImplementedException();
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcRpInitiatedLogoutDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().EndSessionResponseAsync(sequenceData.DownPartyLink.Id);
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyLink.Id, sessionIndex: sequenceData.SessionId);
                    case PartyTypes.TrackLink:
                        return await serviceProvider.GetService<TrackLinkRpInitiatedLogoutDownLogic>().LogoutResponseAsync(sequenceData.DownPartyLink.Id);

                    default:
                        throw new NotSupportedException();
                }
            }
            catch (Exception ex)
            {
                throw new StopSequenceException("Falling logout response down", ex);
            }
        }
    }
}
