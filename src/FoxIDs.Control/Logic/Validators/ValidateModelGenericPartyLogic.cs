using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Logic
{
    public class ValidateModelGenericPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly UpPartyCacheLogic upPartyCacheLogic;
        private readonly ClaimTransformValidationLogic claimTransformValidationLogic;

        public ValidateModelGenericPartyLogic(TelemetryScopedLogger logger, UpPartyCacheLogic upPartyCacheLogic, ClaimTransformValidationLogic claimTransformValidationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.upPartyCacheLogic = upPartyCacheLogic;
            this.claimTransformValidationLogic = claimTransformValidationLogic;
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
                        upPartyLink.DisplayName = upParty.DisplayName;
                        upPartyLink.Type = upParty.Type;
                        upPartyLink.Issuers = upParty.Issuers;
                        upPartyLink.SpIssuer = upParty.SpIssuer;
                        upPartyLink.HrdDomains = upParty.HrdDomains;
                        upPartyLink.HrdShowButtonWithDomain = upParty.HrdShowButtonWithDomain;
                        upPartyLink.HrdDisplayName = upParty.HrdDisplayName;
                        upPartyLink.HrdLogoUrl = upParty.HrdLogoUrl;
                        upPartyLink.DisableUserAuthenticationTrust = upParty.DisableUserAuthenticationTrust;
                        upPartyLink.DisableTokenExchangeTrust = upParty.DisableTokenExchangeTrust;
                    }
                    catch (FoxIDsDataException ex)
                    {
                        if (ex.StatusCode == DataStatusCode.NotFound)
                        {
                            isValid = false;
                            var errorMessage = $"Allow authentication method '{upPartyLink.Name}' not found.";
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

        public bool ValidateModelUpPartyProfiles(ModelStateDictionary modelState, UpParty upParty)
        {
            var isValid = true;
            try
            {
                if (upParty is OidcUpParty oidcUpParty)
                {
                    var duplicatedName = oidcUpParty.Profiles?.GroupBy(ct => ct.Name).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    if (!string.IsNullOrEmpty(duplicatedName))
                    {
                        throw new ValidationException($"Duplicated profile name '{duplicatedName}'");
                    }
                }
                else if (upParty is SamlUpParty samlUpParty)
                {
                    var duplicatedName = samlUpParty.Profiles?.GroupBy(ct => ct.Name).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    if (!string.IsNullOrEmpty(duplicatedName))
                    {
                        throw new ValidationException($"Duplicated profile name '{duplicatedName}'");
                    }
                }
                else if (upParty is TrackLinkUpParty trackLinkUpParty && trackLinkUpParty.Profiles != null)
                {
                    var duplicatedName = trackLinkUpParty.Profiles?.GroupBy(ct => ct.Name).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    if (!string.IsNullOrEmpty(duplicatedName))
                    {
                        throw new ValidationException($"Duplicated profile name '{duplicatedName}'");
                    }
                }
            }
            catch (ValidationException vex)
            {
                isValid = false;
                logger.Warning(vex);
                modelState.TryAddModelError($"{nameof(OidcUpParty.Profiles)}.{nameof(UpPartyProfile.Name)}".ToCamelCase(), vex.Message);
            }
            return isValid;
        }
    }
}
