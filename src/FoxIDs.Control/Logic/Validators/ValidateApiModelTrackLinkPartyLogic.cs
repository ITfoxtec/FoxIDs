using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoxIDs.Logic
{
    public class ValidateApiModelTrackLinkPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic;
        private readonly ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic;

        public ValidateApiModelTrackLinkPartyLogic(TelemetryScopedLogger logger, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.validateApiModelGenericPartyLogic = validateApiModelGenericPartyLogic;
            this.validateApiModelDynamicElementLogic = validateApiModelDynamicElementLogic;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.TrackLinkUpParty party)
        {
            return validateApiModelGenericPartyLogic.ValidateExtendedUi(modelState, party.ExtendedUis) &&
                validateApiModelDynamicElementLogic.ValidateApiModelLinkExternalUserElements(modelState, party.LinkExternalUser?.Elements);
        }
    }
}
