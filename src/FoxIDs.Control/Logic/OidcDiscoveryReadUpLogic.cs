using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcDiscoveryReadUpLogic<MParty, MClient> : LogicBase where MParty : OAuthUpParty<MClient> where MClient : OAuthUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly OidcDiscoveryReadModelLogic<MParty, MClient> oidcDiscoveryReadModelLogic;

        public OidcDiscoveryReadUpLogic(TelemetryScopedLogger logger, OidcDiscoveryReadModelLogic<MParty, MClient> oidcDiscoveryReadModelLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.oidcDiscoveryReadModelLogic = oidcDiscoveryReadModelLogic;
        }

        public async Task<bool> PopulateModelAsync(ModelStateDictionary modelState, MParty mp)
        {
            var isValid = true;
            try
            {
                if (mp.UpdateState != PartyUpdateStates.Manual)
                {
                    _ = await oidcDiscoveryReadModelLogic.PopulateModelAsync(mp);

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
