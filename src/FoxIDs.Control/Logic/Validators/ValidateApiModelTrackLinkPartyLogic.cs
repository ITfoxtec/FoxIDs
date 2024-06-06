using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoxIDs.Logic
{
    public class ValidateApiModelTrackLinkPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic;

        public ValidateApiModelTrackLinkPartyLogic(TelemetryScopedLogger logger, ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.validateApiModelDynamicElementLogic = validateApiModelDynamicElementLogic;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.TrackLinkUpParty party)
        {
            return validateApiModelDynamicElementLogic.ValidateApiModelLinkExternalUserElements(modelState, party.LinkExternalUser?.Elements);
        }
    }
}
