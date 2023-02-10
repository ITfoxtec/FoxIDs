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
using System;

namespace FoxIDs.Logic
{
    public class ValidateGenericPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly UpPartyCacheLogic upPartyCacheLogic;
        private readonly ClaimTransformValidationLogic claimTransformValidationLogic;

        public ValidateGenericPartyLogic(TelemetryScopedLogger logger, UpPartyCacheLogic upPartyCacheLogic, ClaimTransformValidationLogic claimTransformValidationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.upPartyCacheLogic = upPartyCacheLogic;
            this.claimTransformValidationLogic = claimTransformValidationLogic;
        }

        public bool ValidateApiModelClaimTransforms<T>(ModelStateDictionary modelState, List<T> claimTransforms) where T : Api.ClaimTransform
        {
            var isValid = true;
            try
            {
                if (claimTransforms?.Count() > 0)
                {
                    var duplicatedOrderNumber = claimTransforms.GroupBy(ct => ct.Order as int?).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    if (duplicatedOrderNumber >= 0)
                    {
                        throw new ValidationException($"Duplicated claim transform order number '{duplicatedOrderNumber}'");
                    }
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
                        var upParty = await upPartyCacheLogic.GetUpPartyAsync(upPartyLink.Name);
                        upPartyLink.Type = upParty.Type;
                        upPartyLink.HrdDomains = upParty.HrdDomains;
                        upPartyLink.HrdShowButtonWithDomain = upParty.HrdShowButtonWithDomain;
                        upPartyLink.HrdDisplayName = upParty.HrdDisplayName;
                        upPartyLink.HrdLogoUrl = upParty.HrdLogoUrl;
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
                downParty.AllowUpParties = downParty.AllowUpParties.OrderBy(up => up.Type).ThenBy(up => up.Name).ToList();
            }
            return isValid;
        }

        public bool ValidateModelClaimTransforms<MParty>(ModelStateDictionary modelState, MParty mParty) where MParty : Party
        {
            if (mParty is LoginUpParty loginUpParty)
            {
                return ValidateModelClaimTransforms(modelState, loginUpParty.ClaimTransforms);
            }
            else if (mParty is OAuthUpParty oauthUpParty)
            {
                return ValidateModelClaimTransforms(modelState, oauthUpParty.ClaimTransforms);
            }
            else if (mParty is SamlUpParty samlUpParty)
            {
                return ValidateModelClaimTransforms(modelState, samlUpParty.ClaimTransforms);
            }
            else if (mParty is OAuthDownParty oauthDownParty)
            {
                return ValidateModelClaimTransforms(modelState, oauthDownParty.ClaimTransforms);
            }
            else if (mParty is SamlDownParty samlDownParty)
            {
                return ValidateModelClaimTransforms(modelState, samlDownParty.ClaimTransforms);
            }

            return true;
        }

        public bool ValidateModelClaimTransforms<MClaimTransform>(ModelStateDictionary modelState, List<MClaimTransform> claimTransforms) where MClaimTransform : ClaimTransform
        {
            if (claimTransforms != null)
            {
                claimTransformValidationLogic.ValidateAndPrepareClaimTransforms(claimTransforms);

                foreach (var claimTransform in claimTransforms)
                {
                    if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
                    {
                        switch (claimTransform.Type)
                        {
                            case ClaimTransformTypes.Constant:
                                claimTransform.ClaimsIn = null;
                                claimTransform.TransformationExtension = null;
                                break;

                            case ClaimTransformTypes.MatchClaim:
                                claimTransform.TransformationExtension = null;
                                break;

                            case ClaimTransformTypes.Match:
                            case ClaimTransformTypes.RegexMatch:
                                break;

                            case ClaimTransformTypes.Map:
                            case ClaimTransformTypes.DkPrivilege:
                                claimTransform.Transformation = null;
                                claimTransform.TransformationExtension = null;
                                break;

                            case ClaimTransformTypes.RegexMap:
                                claimTransform.TransformationExtension = null;
                                break;

                            case ClaimTransformTypes.Concatenate:
                                claimTransform.TransformationExtension = null;
                                break;

                            default:
                                throw new NotSupportedException($"Claim transformation type '{claimTransform.Type}' not supported.");
                        }
                    }
                    else if (claimTransform.Action == ClaimTransformActions.AddIfNot || claimTransform.Action == ClaimTransformActions.ReplaceIfNot)
                    {
                        switch (claimTransform.Type)
                        {
                            case ClaimTransformTypes.MatchClaim:
                                claimTransform.TransformationExtension = null;
                                break;

                            case ClaimTransformTypes.Match:
                            case ClaimTransformTypes.RegexMatch:
                                break;

                            default:
                                throw new NotSupportedException($"Claim transformation type '{claimTransform.Type}' not supported.");
                        }
                    }
                    else if (claimTransform.Action == ClaimTransformActions.AddIfNotOut)
                    {
                        switch (claimTransform.Type)
                        {
                            case ClaimTransformTypes.Map:
                                claimTransform.Transformation = null;
                                claimTransform.TransformationExtension = null;
                                break;

                            case ClaimTransformTypes.RegexMap:
                                claimTransform.TransformationExtension = null;
                                break;

                            default:
                                throw new NotSupportedException($"Claim transformation type '{claimTransform.Type}' not supported.");
                        }
                    }
                    else if (claimTransform.Action == ClaimTransformActions.Remove)
                    {
                        switch (claimTransform.Type)
                        {
                            case ClaimTransformTypes.MatchClaim:
                                claimTransform.ClaimsIn = null;
                                claimTransform.Transformation = null;
                                claimTransform.TransformationExtension = null;
                                break;

                            case ClaimTransformTypes.Match:
                            case ClaimTransformTypes.RegexMatch:
                                claimTransform.ClaimsIn = null;
                                claimTransform.TransformationExtension = null;
                                break;

                            default:
                                throw new NotSupportedException($"Claim transformation type '{claimTransform.Type}' not supported.");
                        }
                    }
                }
            }

            return true;
        }
    }
}
