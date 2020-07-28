﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ValidateOAuthLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantService;

        public ValidateOAuthLogic(TelemetryScopedLogger logger, ITenantRepository tenantService, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantService = tenantService;
        }

        // validate response type

        public async Task<bool> ValidateResourceScopesAsync<TClient, TScope, TClaim>(ModelStateDictionary modelState, OAuthDownParty<TClient, TScope, TClaim> oauthDownParty) where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
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
