﻿using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
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
            return validateApiModelGenericPartyLogic.ValidateApiModelClaimTransforms(modelState, party.ExitClaimTransforms, errorFieldName: nameof(Api.ExternalLoginUpParty.ExitClaimTransforms)) &&
                validateApiModelGenericPartyLogic.ValidateExtendedUi(modelState, party.ExtendedUis) &&
                validateApiModelDynamicElementLogic.ValidateApiModelLinkExternalUserElements(modelState, party.LinkExternalUser?.Elements) &&
                validateApiModelGenericPartyLogic.ValidateApiModelClaimTransforms(modelState, party.LinkExternalUser?.ClaimTransforms, errorFieldName: nameof(Api.ExternalLoginUpParty.LinkExternalUser.ClaimTransforms)) &&
                validateApiModelGenericPartyLogic.ValidateApiModelHrdIPAddressesAndRanges(modelState, party.HrdIPAddressesAndRanges, errorFieldName: nameof(Api.ExternalLoginUpParty.HrdIPAddressesAndRanges)) &&
                validateApiModelGenericPartyLogic.ValidateApiModelHrdRegularExpressions(modelState, party.HrdRegularExpressions, errorFieldName: nameof(Api.ExternalLoginUpParty.HrdRegularExpressions));
        }
    }
}
