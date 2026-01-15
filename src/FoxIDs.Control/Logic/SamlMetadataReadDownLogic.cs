using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SamlMetadataReadDownLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly SamlMetadataReadLogic samlMetadataReadLogic;

        public SamlMetadataReadDownLogic(TelemetryScopedLogger logger, SamlMetadataReadLogic samlMetadataReadLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.samlMetadataReadLogic = samlMetadataReadLogic;
        }

        public async Task<bool> PopulateModelAsync(ModelStateDictionary modelState, SamlDownParty party)
        {
            var isValid = true;
            try
            {
                if (party.UpdateState != PartyUpdateStates.Manual)
                {
                    _ = await samlMetadataReadLogic.PopulateModelAsync(party);

                    if (party.UpdateState == PartyUpdateStates.AutomaticStopped)
                    {
                        party.UpdateState = PartyUpdateStates.Automatic;
                    }
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                logger.Warning(ex);
                modelState.TryAddModelError(nameof(party.MetadataUrl).ToCamelCase(), ex.GetAllMessagesJoined());
            }
            return isValid;
        }
    }
}
