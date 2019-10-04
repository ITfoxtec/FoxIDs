using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ValidatePartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantService;

        public ValidatePartyLogic(TelemetryScopedLogger logger, ITenantRepository tenantService, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantService = tenantService;
        }

        public async Task<bool> ValidateAllowUpParties(ModelStateDictionary modelState, string propertyName, DownParty downParty)
        {
            var isValid = true;
            if(downParty.AllowUpParties?.Count() > 0)
            {
                foreach(var upPartyLink in downParty.AllowUpParties)
                {
                    try
                    {
                        var upParty = await tenantService.GetAsync<UpParty>(await UpParty.IdFormat(RouteBinding, upPartyLink.Name));
                        upPartyLink.Type = upParty.Type;
                    }
                    catch (CosmosDataException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.NotFound)
                        {
                            isValid = false;
                            var errorMessage = $"Allow up party '{upPartyLink.Name}' not found.";
                            logger.Warning(ex, errorMessage);
                            modelState.TryAddModelError(propertyName.ToCamelCase(), errorMessage);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            return isValid;
        }

        public async Task<bool> ValidateResourceScopes<TClient, TScope, TClaim>(ModelStateDictionary modelState, OAuthDownParty<TClient, TScope, TClaim> oauthDownParty) where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
        {
            var isValid = true;
            if (oauthDownParty.Client?.ResourceScopes?.Count() > 0)
            {
                foreach (var resourceScope in oauthDownParty.Client.ResourceScopes.Where(rs => !rs.Resource.Equals(oauthDownParty.Name, System.StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        _ = await tenantService.GetAsync<DownParty>(await DownParty.IdFormat(RouteBinding, resourceScope.Resource));
                    }
                    catch (CosmosDataException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.NotFound)
                        {
                            isValid = false;
                            var errorMessage = $"Resource scope down party resource '{resourceScope.Resource}' not found.";
                            logger.Warning(ex, errorMessage);
                            modelState.TryAddModelError($"{nameof(oauthDownParty.Client)}.{nameof(oauthDownParty.Client.ResourceScopes)}".ToCamelCase(), errorMessage);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            return isValid;
        }
    }
}
