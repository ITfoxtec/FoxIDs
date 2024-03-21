using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.ViewModels;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class ExtController : EndpointController 
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IStringLocalizer localizer;
        private readonly ITenantRepository tenantRepository;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly ExternalUserLogic externalUserLogic;
        private readonly DynamicElementLogic dynamicElementLogic;

        public ExtController(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IStringLocalizer localizer, ITenantRepository tenantRepository, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, ExternalUserLogic externalUserLogic, DynamicElementLogic dynamicElementLogic) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.localizer = localizer;
            this.tenantRepository = tenantRepository;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.externalUserLogic = externalUserLogic;
            this.dynamicElementLogic = dynamicElementLogic;
        }


        public async Task<IActionResult> CreateUser()
        {
            try
            {
                logger.ScopeTrace(() => "Start external create user.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalUserUpSequenceData>(remove: false);
                var externalUserUpParty = await tenantRepository.GetAsync<ExternalUserUpParty>(sequenceData.UpPartyId);
                if (!(externalUserUpParty.LinkExternalUser?.AutoCreateUser == true))
                {
                    throw new InvalidOperationException("Automatic create external user not enabled.");
                }

                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(await sequenceLogic.GetUiUpPartyIdAsync());
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                logger.ScopeTrace(() => "Show create external user dialog.");
                return View(nameof(CreateUser), new CreateExternalUserViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    Elements = dynamicElementLogic.ToElementsViewModel(externalUserUpParty.LinkExternalUser.Elements, initClaims: sequenceData.Claims?.ToClaimList()).ToList()
                });

            }
            catch (Exception ex)
            {
                throw new EndpointException($"Create external user failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateExternalUserViewModel createExternalUser)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalUserUpSequenceData>(remove: false);
                var externalUserUpParty = await tenantRepository.GetAsync<ExternalUserUpParty>(sequenceData.UpPartyId);
                if (!(externalUserUpParty.LinkExternalUser?.AutoCreateUser == true))
                {
                    throw new InvalidOperationException("Automatic create external user not enabled.");
                }

                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(await sequenceLogic.GetUiUpPartyIdAsync());
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                createExternalUser.Elements = dynamicElementLogic.ToElementsViewModel(externalUserUpParty.LinkExternalUser.Elements, valueElements: createExternalUser.Elements).ToList();

                Func<IActionResult> viewError = () =>
                {
                    createExternalUser.SequenceString = SequenceString;
                    createExternalUser.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    createExternalUser.IconUrl = loginUpParty.IconUrl;
                    createExternalUser.Css = loginUpParty.Css;
                    return View(nameof(CreateUser), createExternalUser);
                };

                ModelState.Clear();
                await dynamicElementLogic.ValidateViewModelElementsAsync(ModelState, createExternalUser.Elements);
                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Create external user post.");

                var dynamicElementClaims = dynamicElementLogic.GetClaims(createExternalUser.Elements);
                var externalAccountClaims = await externalUserLogic.CreateUserAsync(externalUserUpParty, sequenceData.LinkClaimValue, dynamicElementClaims);

                await sequenceLogic.RemoveSequenceDataAsync<ExternalUserUpSequenceData>();
                switch (sequenceData.UpPartyType)
                {
                    case PartyTypes.Oidc:
                        throw new NotImplementedException();
                    case PartyTypes.Saml2:
                        return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnResponsePostAsync(sequenceData, externalAccountClaims);
                    case PartyTypes.TrackLink:
                        throw new NotImplementedException();
                    default:
                        throw new NotSupportedException();
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Create external user failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
