using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SamlMetadataReadUpLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly SamlMetadataReadLogic samlMetadataReadLogic;

        public SamlMetadataReadUpLogic(TelemetryScopedLogger logger, SamlMetadataReadLogic samlMetadataReadLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.samlMetadataReadLogic = samlMetadataReadLogic;
        }

        public async Task<bool> PopulateModelAsync(ModelStateDictionary modelState, SamlUpParty mp)
        {
            var isValid = true;
            try
            {
                if (mp.UpdateState != PartyUpdateStates.Manual)
                {
                    _ = await samlMetadataReadLogic.PopulateModelAsync(mp);

                    if (mp.UpdateState == PartyUpdateStates.AutomaticStopped)
                    {
                        mp.UpdateState = PartyUpdateStates.Automatic;
                    }
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                logger.Warning(ex);
                modelState.TryAddModelError(nameof(mp.MetadataUrl).ToCamelCase(), ex.GetAllMessagesJoined());
            }
            return isValid;
        }
    }
}
