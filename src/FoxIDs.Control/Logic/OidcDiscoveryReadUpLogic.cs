using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcDiscoveryReadUpLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly OidcDiscoveryReadLogic oidcDiscoveryReadLogic;

        public OidcDiscoveryReadUpLogic(TelemetryScopedLogger logger, OidcDiscoveryReadLogic oidcDiscoveryReadLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.oidcDiscoveryReadLogic = oidcDiscoveryReadLogic;
        }

        public async Task<bool> PopulateModelAsync(ModelStateDictionary modelState, OidcUpParty mp)
        {
            var isValid = true;
            try
            {
                if (mp.UpdateState != PartyUpdateStates.Manual)
                {
                    await oidcDiscoveryReadLogic.PopulateModelAsync(mp);

                    if(mp.UpdateState == PartyUpdateStates.AutomaticStopped)
                    {
                        mp.UpdateState = PartyUpdateStates.Automatic;
                    }

                    if(mp.EditIssuersInAutomatic == false)
                    {
                        mp.EditIssuersInAutomatic = null;
                    }
                }
                else
                {
                    mp.EditIssuersInAutomatic = null;
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                logger.Warning(ex);
                modelState.TryAddModelError(nameof(mp.Authority).ToCamelCase(), ex.GetAllMessagesJoined());
            }
            return isValid;
        }
    }
}
