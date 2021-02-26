using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Linq;
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
                (var oidcDiscovery, var jsonWebKeySet) = await oidcDiscoveryReadLogic.GetOidcDiscoveryAndValidateAsync(mp.Authority);

                mp.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                mp.Issuer = oidcDiscovery.Issuer;
                mp.Client.AuthorizeUrl = oidcDiscovery.AuthorizationEndpoint;
                mp.Client.TokenUrl = oidcDiscovery.TokenEndpoint;
                if (!oidcDiscovery.EndSessionEndpoint.IsNullOrEmpty())
                {
                    mp.Client.EndSessionUrl = oidcDiscovery.EndSessionEndpoint;
                }
                mp.Keys = jsonWebKeySet.Keys?.ToList();
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
