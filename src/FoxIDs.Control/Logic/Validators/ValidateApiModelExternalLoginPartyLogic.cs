using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoxIDs.Logic
{
    public class ValidateApiModelExternalLoginPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic;
        private readonly ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic;

        public ValidateApiModelExternalLoginPartyLogic(TelemetryScopedLogger logger, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.validateApiModelGenericPartyLogic = validateApiModelGenericPartyLogic;
            this.validateApiModelDynamicElementLogic = validateApiModelDynamicElementLogic;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.ExternalLoginUpParty party)
        {
            var isValid = true;

            if (!ValidateApiModelLoginPartyLogic.TryValidateAndSanitizeCss(modelState, logger, nameof(Api.ExternalLoginUpParty.Css), party.Css, out var sanitizedCss))
            {
                isValid = false;
            }
            else
            {
                party.Css = sanitizedCss;
            }

            if (!ValidateApiModelLoginPartyLogic.TryValidateIconUrl(modelState, logger, nameof(Api.ExternalLoginUpParty.IconUrl), party.IconUrl))
            {
                isValid = false;
            }

            if (party.Title.IsNullOrWhiteSpace())
            {
                party.Title = RouteBinding.DisplayName;
            }

            if (!validateApiModelGenericPartyLogic.ValidateApiModelClaimTransforms(modelState, party.ExitClaimTransforms, errorFieldName: nameof(Api.ExternalLoginUpParty.ExitClaimTransforms)))
            {
                isValid = false;
            }

            if (!validateApiModelGenericPartyLogic.ValidateExtendedUi(modelState, party.ExtendedUis))
            {
                isValid = false;
            }

            if (!validateApiModelDynamicElementLogic.ValidateApiModelLinkExternalUserElements(modelState, party.LinkExternalUser?.Elements))
            {
                isValid = false;
            }

            if (!validateApiModelGenericPartyLogic.ValidateApiModelClaimTransforms(modelState, party.LinkExternalUser?.ClaimTransforms, errorFieldName: nameof(Api.ExternalLoginUpParty.LinkExternalUser.ClaimTransforms)))
            {
                isValid = false;
            }

            if (!validateApiModelGenericPartyLogic.ValidateApiModelHrdIPAddressesAndRanges(modelState, party.HrdIPAddressesAndRanges, errorFieldName: nameof(Api.ExternalLoginUpParty.HrdIPAddressesAndRanges)))
            {
                isValid = false;
            }

            if (!validateApiModelGenericPartyLogic.ValidateApiModelHrdRegularExpressions(modelState, party.HrdRegularExpressions, errorFieldName: nameof(Api.ExternalLoginUpParty.HrdRegularExpressions)))
            {
                isValid = false;
            }

            return isValid;
        }
    }
}
