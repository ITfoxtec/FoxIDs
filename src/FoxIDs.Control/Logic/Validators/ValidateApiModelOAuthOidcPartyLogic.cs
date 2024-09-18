using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
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
    public class ValidateApiModelOAuthOidcPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantService;
        private readonly ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic;

        public ValidateApiModelOAuthOidcPartyLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantService, ValidateApiModelDynamicElementLogic validateApiModelDynamicElementLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantService = tenantService;
            this.validateApiModelDynamicElementLogic = validateApiModelDynamicElementLogic;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.OAuthUpParty party)
        {
            var isValid = true;

            // Space to validate

            return isValid;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.OidcUpParty party)
        {
            var isValid = true;

            (var isValidRtResult, string responseType) = ValidateResponseTypeUp(modelState, party.Client.ResponseType, Constants.Oidc.DefaultResponseTypes);
            if (isValidRtResult)
            {
                party.Client.ResponseType = responseType;
            }
            else
            {
                isValid = isValidRtResult;
            }

            var isValidRmResult = ValidateResponseModeUp(modelState, party.Client.ResponseMode);
            if (!isValidRmResult)
            {
                isValid = isValidRmResult;
            }

            if (party.Client.UseUserInfoClaims && party.Client.UseIdTokenClaims)
            {
                party.Client.UseIdTokenClaims = false;
            }

            if(!validateApiModelDynamicElementLogic.ValidateApiModelLinkExternalUserElements(modelState, party.LinkExternalUser?.Elements))
            {
                isValid = false;
            }

            return isValid;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.OAuthDownParty party)
        {
            var isValid = true;
            if (party.Client != null)
            {
                (var isValidRtResult, List<string> responseTypes) = ValidateResponseTypesDown(modelState, party.Client.ResponseTypes, Constants.OAuth.DefaultResponseTypes);
                if (isValidRtResult)
                {
                    party.Client.ResponseTypes = responseTypes;
                }
                else 
                {
                    isValid = isValidRtResult;
                }

                if(party.Resource == null && party.Client.ResourceScopes?.Count() > 0)
                {
                    foreach (var rs in party.Client.ResourceScopes)
                    {
                        if (rs.Resource == party.Name || (!party.NewName.IsNullOrWhiteSpace() && rs.Resource == party.NewName))
                        {
                            rs.Scopes = null;
                        }
                    }
                }
            }
            return isValid;
        }

        public bool ValidateApiModel(ModelStateDictionary modelState, Api.OidcDownParty party)
        {
            var isValid = true;
            if (party.Client != null)
            {
                (var isValidRtResult, List<string> responseTypes) = ValidateResponseTypesDown(modelState, party.Client.ResponseTypes, Constants.Oidc.DefaultResponseTypes);
                if (isValidRtResult)
                {
                    party.Client.ResponseTypes = responseTypes;
                }
                else
                {
                    isValid = isValidRtResult;
                }

                if (party.Resource == null && party.Client.ResourceScopes?.Count() > 0)
                {
                    foreach (var rs in party.Client.ResourceScopes)
                    {
                        if (rs.Resource == party.Name || (!party.NewName.IsNullOrWhiteSpace() && rs.Resource == party.NewName))
                        {
                            rs.Scopes = null;
                        }
                    }
                }
            }
            return isValid;
        }

        public async Task<bool> ValidateModelAsync<TClient, TScope, TClaim>(ModelStateDictionary modelState, OAuthDownParty<TClient, TScope, TClaim> oauthDownParty) where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
        {
            return await ValidateClientResourceScopesAsync(modelState, oauthDownParty) &&
                ValidateClientScopes(modelState, oauthDownParty) &&
                ValidateClientResourceScopes(modelState, oauthDownParty) &&
                ValidateResourceScopes(modelState, oauthDownParty);
        }

        private bool ValidateResponseModeUp(ModelStateDictionary modelState, string responseMode)
        {
            var isValid = true;
            try
            {
                if (!responseMode.IsNullOrWhiteSpace())
                {
                    var validResponseMode = new string[] { IdentityConstants.ResponseModes.FormPost, IdentityConstants.ResponseModes.Query };
                    if (!validResponseMode.Contains(responseMode))
                    {
                        throw new ValidationException($"Not supported response mode '{responseMode}'");
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError($"{nameof(Api.OAuthDownParty.Client)}.{nameof(Api.OAuthDownParty.Client.ResponseTypes)}".ToCamelCase(), vex.Message);
            }
            return isValid;
        }

        private (bool, string) ValidateResponseTypeUp(ModelStateDictionary modelState, string responseType, string[] defaultResponseTypes)
        {
            var isValid = true;
            try
            {
                if (!responseType.IsNullOrWhiteSpace())
                {
                    responseType = OrderResponseType(responseType);

                    if (!defaultResponseTypes.Contains(responseType))
                    {
                        throw new ValidationException($"Not supported response type '{responseType}'");
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError($"{nameof(Api.OAuthDownParty.Client)}.{nameof(Api.OAuthDownParty.Client.ResponseTypes)}".ToCamelCase(), vex.Message);
            }
            return (isValid, responseType);
        }

        private (bool, List<string>) ValidateResponseTypesDown(ModelStateDictionary modelState, List<string> responseTypes, string[] defaultResponseTypes)
        {
            var isValid = true;
            try
            {
                if (responseTypes?.Count > 0)
                {
                    responseTypes = OrderResponseTypes(responseTypes);

                    foreach (var responseType in responseTypes.Select(rt => rt.ToSpaceList()))
                    {
                        if (responseType.GroupBy(rt => rt).Where(g => g.Count() > 1).Any())
                        {
                            throw new ValidationException($"Invalid response type '{responseType.ToSpaceList()}'");
                        }

                        var responseTypeString = responseType.ToSpaceList();
                        if (!defaultResponseTypes.Contains(responseTypeString))
                        {
                            throw new ValidationException($"Not supported response type '{responseTypeString}'");
                        }

                        var duplicatedResponseType = responseType.GroupBy(rt => rt).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                        if (duplicatedResponseType != null)
                        {
                            throw new ValidationException($"Duplicated response type '{duplicatedResponseType}'.");
                        }
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError($"{nameof(Api.OAuthDownParty.Client)}.{nameof(Api.OAuthDownParty.Client.ResponseTypes)}".ToCamelCase(), vex.Message);
            }
            return (isValid, responseTypes);
        }

        private List<string> OrderResponseTypes(List<string> responseTypes)
        {
            return responseTypes.Select(OrderResponseType).ToList();
        }

        private string OrderResponseType(string responseType)
        {
            var orderedResponseType = responseType.ToSpaceList()
                .OrderBy(rt => Array.IndexOf([IdentityConstants.ResponseTypes.Code, IdentityConstants.ResponseTypes.Token, IdentityConstants.ResponseTypes.IdToken], rt));

            return orderedResponseType.ToSpaceList();
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

                    foreach (var resourceScope in oauthDownParty.Client.ResourceScopes.Where(rs => !rs.Resource.Equals(oauthDownParty.Name, StringComparison.Ordinal)))
                    {
                        var duplicatedScope = resourceScope.Scopes?.GroupBy(s => s).Where(g => g.Count() > 1).FirstOrDefault();
                        if (duplicatedScope != null)
                        {
                            throw new ValidationException($"Duplicated scope in resource scope, resource '{resourceScope.Resource} scope '{duplicatedScope.Key}'.");
                        }
                        try
                        {
                            // Test if application registration exists.
                            var resourceDownParty = await tenantService.GetAsync<OAuthDownParty>(await DownParty.IdFormatAsync(RouteBinding, resourceScope.Resource));
                            if (resourceScope.Scopes?.Count > 0)
                            {
                                foreach (var scope in resourceScope.Scopes)
                                {
                                    if (!(resourceDownParty.Resource?.Scopes?.Where(s => s.Equals(scope, StringComparison.Ordinal)).Count() > 0))
                                    {
                                        throw new ValidationException($"Resource '{resourceScope.Resource}' scope '{scope}' not found.");
                                    }
                                }
                            }
                        }
                        catch (FoxIDsDataException ex)
                        {
                            if (ex.StatusCode == DataStatusCode.NotFound)
                            {
                                isValid = false;
                                var errorMessage = $"Resource scope application registration resource '{resourceScope.Resource}' not found.";
                                logger.Warning(ex, errorMessage);
                                modelState.TryAddModelError($"{nameof(oauthDownParty.Client)}.{nameof(oauthDownParty.Client.ResourceScopes)}".ToCamelCase(), errorMessage);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }

                    var appResourceScope = oauthDownParty.Client.ResourceScopes.Where(rs => rs.Resource.Equals(oauthDownParty.Name, StringComparison.Ordinal)).SingleOrDefault();
                    if (appResourceScope != null && appResourceScope.Scopes?.Count() > 0)
                    {
                        foreach (var scope in appResourceScope.Scopes)
                        {
                            if (!(oauthDownParty.Resource?.Scopes?.Where(s => s.Equals(scope, StringComparison.Ordinal)).Count() > 0))
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

        private bool ValidateClientResourceScopes<TClient, TScope, TClaim>(ModelStateDictionary modelState, OAuthDownParty<TClient, TScope, TClaim> oauthDownParty) where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
        {
            var isValid = true;
            if (oauthDownParty.Client != null && !(oauthDownParty.Client.ResourceScopes?.Count() > 0))
            {
                oauthDownParty.Client.ResourceScopes = new List<OAuthDownResourceScope> { new OAuthDownResourceScope { Resource = oauthDownParty.Name } };
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
