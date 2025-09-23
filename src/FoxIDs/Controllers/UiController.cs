using System;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.ViewModels;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class UiController : EndpointController 
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IStringLocalizer localizer;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ExtendedUiLogic extendedUiLogic;
        private readonly ExtendedUiConnectLogic extendedUiConnectLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly DynamicElementLogic dynamicElementLogic;

        public UiController(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, SequenceLogic sequenceLogic, ClaimTransformLogic claimTransformLogic, ExtendedUiLogic extendedUiLogic, ExtendedUiConnectLogic extendedUiConnectLogic, SecurityHeaderLogic securityHeaderLogic, DynamicElementLogic dynamicElementLogic) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.sequenceLogic = sequenceLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.extendedUiLogic = extendedUiLogic;
            this.extendedUiConnectLogic = extendedUiConnectLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.dynamicElementLogic = dynamicElementLogic;
        }

        public async Task<IActionResult> Ext(int step)
        {
            try
            {
                logger.ScopeTrace(() => "Start extended UI.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExtendedUiUpSequenceData>(remove: false);
                var extendedUiUpParty = await tenantDataRepository.GetAsync<UpParty>(sequenceData.UpPartyId);
                (var extendedUi, var stateString) = await extendedUiLogic.GetExtendedUiAndStateStringAsync(sequenceData, extendedUiUpParty.ExtendedUis, step);

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(await sequenceLogic.GetUiUpPartyIdAsync());
                securityHeaderLogic.AddImgSrc(loginUpParty);
                securityHeaderLogic.AddImgSrcFromDynamicElements(extendedUi.Elements);

                logger.ScopeTrace(() => "Show extended UI dialog.");
                return View(nameof(Ext), new ExtendedUiViewModel
                {
                    SequenceString = SequenceString,
                    State = stateString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    PageTitle = extendedUi.Title,
                    SubmitButtonText = extendedUi.SubmitButtonText,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    InputElements = dynamicElementLogic.ToUiElementsViewModel(extendedUi.Elements, initClaims: sequenceData.Claims?.ToClaimList()).ToList(),
                    Elements = dynamicElementLogic.GetLoginElementsViewModel(loginUpParty)
                });

            }
            catch (Exception ex)
            {
                throw new EndpointException($"Extended UI failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ext(ExtendedUiViewModel extendedUiViewModel)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExtendedUiUpSequenceData>(remove: false);
                var extendedUiUpParty = await tenantDataRepository.GetAsync<UpParty>(sequenceData.UpPartyId);
                (var extendedUi, var step) = await extendedUiLogic.GetExtendedUiAndStepAsync(sequenceData, extendedUiUpParty.ExtendedUis, extendedUiViewModel.State);

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(await sequenceLogic.GetUiUpPartyIdAsync());
                securityHeaderLogic.AddImgSrc(loginUpParty);
                securityHeaderLogic.AddImgSrcFromDynamicElements(extendedUi.Elements);

                extendedUiViewModel.InputElements = dynamicElementLogic.ToUiElementsViewModel(extendedUi.Elements, valueElements: extendedUiViewModel.InputElements).ToList();

                Func<IActionResult> viewError = () =>
                {
                    extendedUiViewModel.SequenceString = SequenceString;
                    extendedUiViewModel.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    extendedUiViewModel.PageTitle = extendedUi.Title;
                    extendedUiViewModel.SubmitButtonText = extendedUi.SubmitButtonText;
                    extendedUiViewModel.IconUrl = loginUpParty.IconUrl;
                    extendedUiViewModel.Css = loginUpParty.Css;
                    extendedUiViewModel.Elements = dynamicElementLogic.GetLoginElementsViewModel(loginUpParty);
                    return View(nameof(Ext), extendedUiViewModel);
                };

                ModelState.Clear();
                await dynamicElementLogic.ValidateViewModelElementsAsync(ModelState, extendedUiViewModel.InputElements);
                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Extended UI post.");
                var claims = step.Claims.ToClaimList();
                if (extendedUi.ExternalConnectType == ExternalConnectTypes.Api)
                {
                    try
                    {
                        var externalClaims = await extendedUiConnectLogic.ValidateElementsAsync(extendedUi, claims, extendedUiViewModel.InputElements);
                        if (externalClaims.Count() > 0)
                        {
                            claims.AddRange(externalClaims);
                        }
                    }
                    catch (InvalidElementsException iex)
                    {
                        logger.ScopeTrace(() => iex.Message, triggerEvent: true);

                        if (iex.Elements.Count() > 0)
                        {
                            foreach (var errorElement in iex.Elements)
                            {
                                dynamicElementLogic.SetModelElementError(ModelState, extendedUiViewModel.InputElements, errorElement.Name, errorElement.UiErrorMessage);
                            }
                        }

                        if (iex.UiErrorMessages.Any())
                        {
                            foreach(var uiErrorMessage in iex.UiErrorMessages)
                            {
                                ModelState.AddModelError(string.Empty, localizer[uiErrorMessage]);
                            }
                        }

                        return viewError();
                    }
                }
                else
                {
                    (var dynamicElementClaims, _) = dynamicElementLogic.GetClaims(extendedUiViewModel.InputElements);
                    if (dynamicElementClaims.Count() > 0)
                    {
                        claims.AddRange(dynamicElementClaims);
                    }
                }

                (var transformedClaims, var actionResult) = await claimTransformLogic.TransformAsync(extendedUi.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims, sequenceData);
                if (actionResult != null)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<ExtendedUiUpSequenceData>();
                    switch (sequenceData.UpPartyType)
                    {
                        case PartyTypes.Login:
                            await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>(partyName: sequenceData.UpPartyId.PartyIdToName());
                            break;
                        case PartyTypes.Oidc:
                            await sequenceLogic.RemoveSequenceDataAsync<OidcUpSequenceData>(partyName: sequenceData.UpPartyId.PartyIdToName());
                            break;
                        case PartyTypes.Saml2:
                            await sequenceLogic.RemoveSequenceDataAsync<SamlUpSequenceData>(partyName: sequenceData.UpPartyId.PartyIdToName());
                            break;
                        case PartyTypes.TrackLink:
                            await sequenceLogic.RemoveSequenceDataAsync<TrackLinkUpSequenceData>(partyName: sequenceData.UpPartyId.PartyIdToName());
                            break;
                        case PartyTypes.ExternalLogin:
                            await sequenceLogic.RemoveSequenceDataAsync<ExternalLoginUpSequenceData>(partyName: sequenceData.UpPartyId.PartyIdToName());
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    return actionResult;
                }

                var nextUiResult = await extendedUiLogic.HandleNextUiAsync(extendedUiUpParty, sequenceData, transformedClaims, extendedUi.Name);
                if (nextUiResult != null)
                {
                    return nextUiResult;
                }

                await sequenceLogic.RemoveSequenceDataAsync<ExtendedUiUpSequenceData>();
                switch (sequenceData.UpPartyType)
                {
                    case PartyTypes.Login:
                        return await serviceProvider.GetService<LoginUpLogic>().LoginResponsePostExtendedUiAsync(sequenceData, transformedClaims);
                    case PartyTypes.Oidc:
                        return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestPostExtendedUiAsync(sequenceData, transformedClaims);
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnResponsePostExtendedUiAsync(sequenceData, transformedClaims);
                    case PartyTypes.TrackLink:
                        return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthResponsePostExtendedUiAsync(sequenceData, transformedClaims);
                    case PartyTypes.ExternalLogin:
                        return await serviceProvider.GetService<ExternalLoginUpLogic>().LoginResponsePostExtendedUiAsync(sequenceData, transformedClaims);
                    default:
                        throw new NotSupportedException();
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Extended UI failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}