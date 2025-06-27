using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ExtendedUiLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly SequenceLogic sequenceLogic;
        private readonly IDataProtectionProvider dataProtectionProvider;

        public ExtendedUiLogic(TelemetryScopedLogger logger, SequenceLogic sequenceLogic, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.sequenceLogic = sequenceLogic;
            this.dataProtectionProvider = dataProtectionProvider;
        }

        public async Task<IActionResult> HandleUiAsync(UpParty party, ILoginRequest loginRequest, IEnumerable<Claim> claims, Action<ExtendedUiUpSequenceData> populateSequenceDataAction)
        {
            if (party.ExtendedUis == null || party.ExtendedUis.Count <= 0)
            {
                return null;
            }

            var extendedUi = GetExtendedUi(party.ExtendedUis, claims);
            if (extendedUi == null)
            {
                return null;
            }

            logger.ScopeTrace(() => $"Redirect to extended UI, Route '{RouteBinding?.Route}'.");
            var sequenceData = new ExtendedUiUpSequenceData(loginRequest)
            {
                UpPartyId = party.Id,
                UpPartyType = party.Type
            };
            sequenceData.Steps.Add(new ExtendedUiStep { Name = extendedUi.Name, Claims = claims.Where(c => c.Type != Constants.JwtClaimTypes.OpenExtendedUi).ToClaimAndValues() });
            populateSequenceDataAction(sequenceData);
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            var query = new Dictionary<string, string> { { Constants.Routes.ExtendedUiStepKey, Convert.ToString(0) } };
            return HttpContext.GetUpPartyUrl(party.Name, Constants.Routes.UiController, Constants.Endpoints.ExtendedUi, includeSequence: true, query: query).ToRedirectResult();
        }

        public async Task<IActionResult> HandleNextUiAsync(UpParty party, ExtendedUiUpSequenceData sequenceData, IEnumerable<Claim> claims, string currentName)
        {
            var nextExtendedUi = GetExtendedUi(party.ExtendedUis, claims, currentName);
            if (nextExtendedUi == null)
            {
                return null;
            }

            logger.ScopeTrace(() => $"Redirect to next extended UI, Route '{RouteBinding?.Route}'.");
            sequenceData.Steps.Add(new ExtendedUiStep { Name = nextExtendedUi.Name, Claims = claims.Where(c => c.Type != Constants.JwtClaimTypes.OpenExtendedUi).ToClaimAndValues() });
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            var query = new Dictionary<string, string> { { Constants.Routes.ExtendedUiStepKey, Convert.ToString(sequenceData.Steps.Count() - 1) } };
            return HttpContext.GetUpPartyUrl(party.Name, Constants.Routes.UiController, Constants.Endpoints.ExtendedUi, includeSequence: true, query: query).ToRedirectResult();
        }

        private ExtendedUi GetExtendedUi(List<ExtendedUi> extendedUis, IEnumerable<Claim> claims, string currentExtendedUiName = null)
        {
            var loadNext = !currentExtendedUiName.IsNullOrEmpty();
            var extendedUiClaim = claims.Where(c => c.Type == Constants.JwtClaimTypes.OpenExtendedUi && (currentExtendedUiName.IsNullOrEmpty() || c.Value != currentExtendedUiName)).FirstOrDefault();
            if (extendedUiClaim != null)
            {
                var extendedUi = extendedUis.Where(e => e.Name == extendedUiClaim.Value).FirstOrDefault();
                logger.ScopeTrace(() => $"{(loadNext ? "Next extended" : "Extended")} UI '{extendedUiClaim.Value}' selected by claim '{Constants.JwtClaimTypes.OpenExtendedUi}'{(extendedUi == null ? " do not exist" : string.Empty)}{(loadNext ? string.Empty : $", Route '{RouteBinding?.Route}'")}.");
                return extendedUi;
            }
            if (!loadNext)
            {
                logger.ScopeTrace(() => $"Extended UI not selected by claim '{Constants.JwtClaimTypes.OpenExtendedUi}', Route '{RouteBinding?.Route}'.");
            }
            return null;
        }

        internal async Task<(ExtendedUi extendedUi, string stateString)> GetExtendedUiAndStateStringAsync(ExtendedUiUpSequenceData sequenceData, List<ExtendedUi> extendedUis, int stepId)
        {
            var step = await GetStepAndCleanStepAsync(sequenceData, stepId);
            var extendedUi = GetExtendedUi(extendedUis, step);
            var stateString = $"{sequenceData.Steps.Count() - 1}.{extendedUi.Name}";

            return (extendedUi, CreateProtector().Protect(stateString));
        }

        private static ExtendedUi GetExtendedUi(List<ExtendedUi> extendedUis, ExtendedUiStep step)
        {
            var extendedUi = extendedUis.Where(e => e.Name == step.Name).FirstOrDefault();
            if (extendedUi == null)
            {
                throw new InvalidOperationException($"Extended UI '{step.Name}' do not exist.");
            }

            return extendedUi;
        }

        private async Task<ExtendedUiStep> GetStepAndCleanStepAsync(ExtendedUiUpSequenceData sequenceData, int stepId, bool cleenUp = true)
        {
            if (sequenceData.Steps.Count() <= stepId)
            {
                throw new InvalidOperationException("Sequence steps and step id from state do not match.");
            }

            if (cleenUp)
            {
                var removeFromStepId = stepId + 1;
                if (sequenceData.Steps.Count() > removeFromStepId)
                {
                    sequenceData.Steps.RemoveRange(removeFromStepId, sequenceData.Steps.Count() - removeFromStepId);
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                }
            }

            return sequenceData.Steps[stepId];
        }

        internal async Task<(ExtendedUi, ExtendedUiStep)> GetExtendedUiAndStepAsync(ExtendedUiUpSequenceData sequenceData, List<ExtendedUi> extendedUis, string state)
        {
            var step = await ReadStateAsync(sequenceData, state);
            return (GetExtendedUi(extendedUis, step), step);
        }

        private async Task<ExtendedUiStep> ReadStateAsync(ExtendedUiUpSequenceData sequenceData, string state)
        {
            var stateString = CreateProtector().Unprotect(state);
            var stateSplit = stateString.Split('.');
            if (stateSplit.Count() != 2)
            {
                throw new InvalidOperationException("Invalid state string");
            }
            var stepId = Convert.ToInt32(stateSplit[0]);
            return await GetStepAndCleanStepAsync(sequenceData, stepId, cleenUp: false);
        }

        private IDataProtector CreateProtector()
        {
            var routeBinding = HttpContext.GetRouteBinding();
            return dataProtectionProvider.CreateProtector([routeBinding.TenantName, routeBinding.TrackName, typeof(ExtendedUiLogic).Name]);
        }
    }
}
