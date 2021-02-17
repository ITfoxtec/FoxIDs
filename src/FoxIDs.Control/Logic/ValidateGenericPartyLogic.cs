using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ValidateGenericPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantService;

        public ValidateGenericPartyLogic(TelemetryScopedLogger logger, ITenantRepository tenantService, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantService = tenantService;
        }

        public bool ValidateApiModelClaimTransforms<T>(ModelStateDictionary modelState, List<T> claimTransforms) where T : Api.ClaimTransform
        {
            var isValid = true;
            try
            {
                var duplicatedOrderNumber = claimTransforms.GroupBy(ct => ct.Order as int?).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                if (duplicatedOrderNumber >= 0)
                {
                    throw new ValidationException($"Duplicated claim transform order number '{duplicatedOrderNumber}'");
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError(nameof(Api.OAuthDownParty.ClaimTransforms).ToCamelCase(), vex.Message);
            }
            return isValid;
        }

        public async Task<bool> ValidateModelAllowUpPartiesAsync(ModelStateDictionary modelState, string propertyName, DownParty downParty)
        {
            var isValid = true;
            if (downParty.AllowUpParties?.Count() > 0)
            {
                foreach (var upPartyLink in downParty.AllowUpParties)
                {
                    try
                    {
                        var upParty = await tenantService.GetAsync<UpParty>(await UpParty.IdFormatAsync(RouteBinding, upPartyLink.Name));
                        upPartyLink.Type = upParty.Type;
                    }
                    catch (CosmosDataException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.NotFound)
                        {
                            isValid = false;
                            var errorMessage = $"Allow up-party '{upPartyLink.Name}' not found.";
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
    }
}
