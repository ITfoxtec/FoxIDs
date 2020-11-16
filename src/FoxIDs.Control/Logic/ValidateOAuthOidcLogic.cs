using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ValidateOAuthOidcLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantService;
        private readonly List<string> acceptedResponseTypeValues = new List<string> { "code", "token", "id_token" };

        public ValidateOAuthOidcLogic(TelemetryScopedLogger logger, ITenantRepository tenantService, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantService = tenantService;
        }

        public async Task<bool> ValidateModelAsync<TClient, TScope, TClaim>(ModelStateDictionary modelState, OAuthDownParty<TClient, TScope, TClaim> oauthDownParty) where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
        {
            return ValidateResponseType(modelState, oauthDownParty) &&
                await ValidateClientResourceScopesAsync(modelState, oauthDownParty) &&
                ValidateClientScopes(modelState, oauthDownParty) && 
                ValidateResourceScopes(modelState, oauthDownParty);
        }

        private bool ValidateResponseType<TClient, TScope, TClaim>(ModelStateDictionary modelState, OAuthDownParty<TClient, TScope, TClaim> oauthDownParty) where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
        {
            var isValid = true;
            if (oauthDownParty.Client != null)
            {
                try
                {
                    var responseTypes = oauthDownParty.Client.ResponseTypes.Select(rt => rt.ToSpaceList());
                    foreach (var responseType in responseTypes)
                    {
                        var duplicatedResponseType = responseType.GroupBy(rt => rt).Where(g => g.Count() > 1).FirstOrDefault();
                        if (duplicatedResponseType != null)
                        {
                            throw new ValidationException($"Duplicated response type value '{duplicatedResponseType.Key}'.");
                        }
                        foreach (var item in responseType)
                        {
                            if (!acceptedResponseTypeValues.Where(v => v.Equals(item, StringComparison.Ordinal)).Any())
                            {
                                throw new ValidationException($"Invalid response type '{responseType}'.");
                            }
                        }
                    }
                }
                catch (ValidationException vex)
                {
                    isValid = false;
                    logger.Warning(vex);
                    modelState.TryAddModelError($"{nameof(oauthDownParty.Client)}.{nameof(oauthDownParty.Client.ResourceScopes)}.{nameof(OAuthDownResourceScope.Scopes)}".ToCamelCase(), vex.Message);
                }
            }
            return isValid;
        }

        private async Task<bool> ValidateClientResourceScopesAsync<TClient, TScope, TClaim>(ModelStateDictionary modelState, OAuthDownParty<TClient, TScope, TClaim> oauthDownParty) where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
        {
            var isValid = true;
            if (oauthDownParty.Client?.ResourceScopes?.Count() > 0)
            {
                try
                {
                    var duplicatedResourceScope = oauthDownParty.Client.ResourceScopes.GroupBy(r => r.Resource).Where(g => g.Count() > 1).FirstOrDefault();
                    if (duplicatedResourceScope != null)
                    {
                        throw new ValidationException($"Duplicated resource scope, resource '{duplicatedResourceScope.Key}'.");
                    }

                    foreach (var resourceScope in oauthDownParty.Client.ResourceScopes.Where(rs => !rs.Resource.Equals(oauthDownParty.Name, System.StringComparison.Ordinal)))
                    {
                        var duplicatedScope = resourceScope.Scopes?.GroupBy(s => s).Where(g => g.Count() > 1).FirstOrDefault();
                        if (duplicatedScope != null)
                        {
                            throw new ValidationException($"Duplicated scope in resource scope, resource '{resourceScope.Resource} scope '{duplicatedScope.Key}'.");
                        }
                        try
                        {
                            // Test if Down-party exists.
                            var resourceDownParty = await tenantService.GetAsync<OAuthDownParty>(await DownParty.IdFormat(RouteBinding, resourceScope.Resource));
                            if (resourceScope.Scopes?.Count > 0)
                            {
                                foreach (var scope in resourceScope.Scopes)
                                {
                                    if (!(resourceDownParty.Resource?.Scopes?.Where(s => s.Equals(scope, System.StringComparison.Ordinal)).Count() > 0))
                                    {
                                        throw new ValidationException($"Resource '{resourceScope.Resource}' scope '{scope}' not found.");
                                    }
                                }
                            }
                        }
                        catch (CosmosDataException ex)
                        {
                            if (ex.StatusCode == HttpStatusCode.NotFound)
                            {
                                isValid = false;
                                var errorMessage = $"Resource scope down-party resource '{resourceScope.Resource}' not found.";
                                logger.Warning(ex, errorMessage);
                                modelState.TryAddModelError($"{nameof(oauthDownParty.Client)}.{nameof(oauthDownParty.Client.ResourceScopes)}".ToCamelCase(), errorMessage);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }

                    var appResourceScope = oauthDownParty.Client.ResourceScopes.Where(rs => rs.Resource.Equals(oauthDownParty.Name, System.StringComparison.Ordinal)).SingleOrDefault();
                    if (appResourceScope != null && appResourceScope.Scopes?.Count() > 0)
                    {
                        foreach (var scope in appResourceScope.Scopes)
                        {
                            if (!(oauthDownParty.Resource?.Scopes?.Where(s => s.Equals(scope, System.StringComparison.Ordinal)).Count() > 0))
                            {
                                if (oauthDownParty.Resource == null)
                                {
                                    oauthDownParty.Resource = new OAuthDownResource { Scopes = new List<string>() };
                                }
                                oauthDownParty.Resource.Scopes.Add(scope);
                            }
                        }
                    }
                }
                catch (ValidationException vex)
                {
                    isValid = false;
                    logger.Warning(vex);
                    modelState.TryAddModelError($"{nameof(oauthDownParty.Client)}.{nameof(oauthDownParty.Client.ResourceScopes)}.{nameof(OAuthDownResourceScope.Scopes)}".ToCamelCase(), vex.Message);
                }
            }
            return isValid;
        }

        private bool ValidateClientScopes<TClient, TScope, TClaim>(ModelStateDictionary modelState, OAuthDownParty<TClient, TScope, TClaim> oauthDownParty) where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
        {
            var isValid = true;
            if (oauthDownParty.Client?.Scopes?.Count() > 0)
            {
                try
                {
                    var duplicatedScope = oauthDownParty.Client.Scopes.GroupBy(s => s).Where(g => g.Count() > 1).FirstOrDefault();
                    if (duplicatedScope != null)
                    {
                        throw new ValidationException($"Duplicated scope in client, scope '{duplicatedScope.Key}'.");
                    }
                    foreach(var scopeItem in oauthDownParty.Client.Scopes)
                    {
                        var duplicatedVoluntaryClaim = scopeItem.VoluntaryClaims?.GroupBy(s => s).Where(g => g.Count() > 1).FirstOrDefault();
                        if (duplicatedVoluntaryClaim != null)
                        {
                            throw new ValidationException($"Duplicated voluntary claim in scope, scope '{scopeItem.Scope} voluntary claim '{duplicatedVoluntaryClaim.Key}'.");
                        }
                    }
                }
                catch (ValidationException vex)
                {
                    isValid = false;
                    logger.Warning(vex);
                    modelState.TryAddModelError($"{nameof(oauthDownParty.Resource)}.{nameof(oauthDownParty.Resource.Scopes)}".ToCamelCase(), vex.Message);
                }
            }
            return isValid;
        }

        private bool ValidateResourceScopes<TClient, TScope, TClaim>(ModelStateDictionary modelState, OAuthDownParty<TClient, TScope, TClaim> oauthDownParty) where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim 
        {
            var isValid = true;
            if (oauthDownParty.Resource?.Scopes?.Count() > 0)
            {
                try
                {
                    var duplicatedScope = oauthDownParty.Resource.Scopes.GroupBy(s => s).Where(g => g.Count() > 1).FirstOrDefault();
                    if (duplicatedScope != null)
                    {
                        throw new ValidationException($"Duplicated scope in resource, scope '{duplicatedScope.Key}'.");
                    }
                }
                catch (ValidationException vex)
                {
                    isValid = false;
                    logger.Warning(vex);
                    modelState.TryAddModelError($"{nameof(oauthDownParty.Resource)}.{nameof(oauthDownParty.Resource.Scopes)}".ToCamelCase(), vex.Message);
                }
            }
            return isValid;
        }
    }
}
