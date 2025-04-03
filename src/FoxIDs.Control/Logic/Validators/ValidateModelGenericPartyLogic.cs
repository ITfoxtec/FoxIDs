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
using ITfoxtec.Identity;

namespace FoxIDs.Logic
{
    public class ValidateModelGenericPartyLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly UpPartyCacheLogic upPartyCacheLogic;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly ClaimTransformValidationLogic claimTransformValidationLogic;

        public ValidateModelGenericPartyLogic(TelemetryScopedLogger logger, UpPartyCacheLogic upPartyCacheLogic, ITenantDataRepository tenantDataRepository, ClaimTransformValidationLogic claimTransformValidationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.upPartyCacheLogic = upPartyCacheLogic;
            this.tenantDataRepository = tenantDataRepository;
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
                        upPartyLink.HrdAlwaysShowButton = upParty.HrdAlwaysShowButton;
                        upPartyLink.HrdDisplayName = upParty.HrdDisplayName;
                        upPartyLink.HrdLogoUrl = upParty.HrdLogoUrl;
                        upPartyLink.DisableUserAuthenticationTrust = upParty.DisableUserAuthenticationTrust;
                        upPartyLink.DisableTokenExchangeTrust = upParty.DisableTokenExchangeTrust;

                        if(!upPartyLink.ProfileName.IsNullOrWhiteSpace())
                        {
                            upPartyLink.ProfileDisplayName = upParty.Profiles?.Where(p => p.Name == upPartyLink.ProfileName).Select(p => p.DisplayName).FirstOrDefault();
                            if(upPartyLink.ProfileDisplayName.IsNullOrWhiteSpace())
                            {
                                isValid = false;
                                var errorMessage = $"Allow authentication method '{upPartyLink.Name}' profile '{upPartyLink.ProfileName}' not found.";
                                try
                                {
                                    throw new Exception(errorMessage);
                                }
                                catch (Exception pex)
                                {
                                    logger.Warning(pex, errorMessage);
                                }
                                modelState.TryAddModelError(propertyName.ToCamelCase(), errorMessage);
                            }
                        }
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

        public async Task<bool> ValidateModelClaimTransformsAsync<MParty>(ModelStateDictionary modelState, MParty mParty) where MParty : Party
        {
            if (mParty is LoginUpParty loginUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, loginUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, loginUpParty.CreateUser?.ClaimTransforms, position: ClaimTransformationPosition.CreateUser);
            }
            else if (mParty is ExternalLoginUpParty externalLoginUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, externalLoginUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, externalLoginUpParty.LinkExternalUser?.ClaimTransforms, position: ClaimTransformationPosition.LinkExternalUser);
            }
            else if (mParty is OAuthUpParty oauthUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, oauthUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, oauthUpParty.LinkExternalUser?.ClaimTransforms, position: ClaimTransformationPosition.LinkExternalUser);
            }
            else if (mParty is OidcUpParty oidchUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, oidchUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, oidchUpParty.LinkExternalUser?.ClaimTransforms, position: ClaimTransformationPosition.LinkExternalUser);
            }
            else if (mParty is SamlUpParty samlUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, samlUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, samlUpParty.LinkExternalUser?.ClaimTransforms, position: ClaimTransformationPosition.LinkExternalUser);
            }
            else if (mParty is TrackLinkUpParty trackLinkUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, trackLinkUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, trackLinkUpParty.LinkExternalUser?.ClaimTransforms, position: ClaimTransformationPosition.LinkExternalUser);
            }
            else if (mParty is OAuthDownParty oauthDownParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, oauthDownParty.ClaimTransforms);
            }
            else if (mParty is OidcDownParty oidcDownParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, oidcDownParty.ClaimTransforms);
            }
            else if (mParty is SamlDownParty samlDownParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, samlDownParty.ClaimTransforms);
            }
            else if (mParty is TrackLinkDownParty trackLinkDownParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, trackLinkDownParty.ClaimTransforms);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private async Task<bool> ValidateModelClaimTransformsAsync<MParty, MClaimTransform>(ModelStateDictionary modelState, MParty mParty, List<MClaimTransform> claimTransforms, ClaimTransformationPosition position = ClaimTransformationPosition.party) where MParty : Party where MClaimTransform : ClaimTransform
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

                            case ClaimTransformTypes.ExternalClaims:
                                claimTransform.ClaimOut = null;
                                claimTransform.Transformation = null;
                                claimTransform.TransformationExtension = null;
                                await HandleClaimTransformationSecretAsync(mParty, claimTransform, position);
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

        private async Task HandleClaimTransformationSecretAsync<MParty, MClaimTransform>(MParty mParty, MClaimTransform claimTransform, ClaimTransformationPosition position) where MParty : Party where MClaimTransform : ClaimTransform
        {
            if(!(claimTransform.Type == ClaimTransformTypes.ExternalClaims && claimTransform.ExternalConnectType == ExternalConnectTypes.Api))
            {
                throw new Exception("Claim transform type and external connect type not supported.");
            }
            if (claimTransform.ApiUrl.IsNullOrWhiteSpace())
            {
                throw new Exception("Claim transform API URL is empty.");
            }

            if (!claimTransform.Secret.IsNullOrWhiteSpace())
            {
                return;
            }

            if (mParty is LoginUpParty)
            {
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(mParty.Id);
                if (position == ClaimTransformationPosition.party)
                {
                    SetSecret(loginUpParty.ClaimTransforms, claimTransform);
                }
                else if (position == ClaimTransformationPosition.CreateUser)
                {
                    SetSecret(loginUpParty.CreateUser?.ClaimTransforms, claimTransform);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (mParty is ExternalLoginUpParty)
            {
                var externalLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(mParty.Id);
                if (position == ClaimTransformationPosition.party)
                {
                    SetSecret(externalLoginUpParty.ClaimTransforms, claimTransform);
                }
                else if (position == ClaimTransformationPosition.LinkExternalUser)
                {
                    SetSecret(externalLoginUpParty.LinkExternalUser.ClaimTransforms, claimTransform);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (mParty is OAuthUpParty)
            {
                var oauthUpParty = await tenantDataRepository.GetAsync<OAuthUpParty>(mParty.Id);
                if (position == ClaimTransformationPosition.party)
                {
                    SetSecret(oauthUpParty.ClaimTransforms, claimTransform);
                }
                else if (position == ClaimTransformationPosition.LinkExternalUser)
                {
                    SetSecret(oauthUpParty.LinkExternalUser.ClaimTransforms, claimTransform);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (mParty is OidcUpParty)
            {
                var oidcUpParty = await tenantDataRepository.GetAsync<OidcUpParty>(mParty.Id);
                if (position == ClaimTransformationPosition.party)
                {
                    SetSecret(oidcUpParty.ClaimTransforms, claimTransform);
                }
                else if (position == ClaimTransformationPosition.LinkExternalUser)
                {
                    SetSecret(oidcUpParty.LinkExternalUser.ClaimTransforms, claimTransform);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (mParty is SamlUpParty)
            {
                var samlUpParty = await tenantDataRepository.GetAsync<SamlUpParty>(mParty.Id);
                if (position == ClaimTransformationPosition.party)
                {
                    SetSecret(samlUpParty.ClaimTransforms, claimTransform);
                }
                else if (position == ClaimTransformationPosition.LinkExternalUser)
                {
                    SetSecret(samlUpParty.LinkExternalUser.ClaimTransforms, claimTransform);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (mParty is TrackLinkUpParty)
            {
                var trackLinkUpParty = await tenantDataRepository.GetAsync<TrackLinkUpParty>(mParty.Id);
                if (position == ClaimTransformationPosition.party)
                {
                    SetSecret(trackLinkUpParty.ClaimTransforms, claimTransform);
                }
                else if (position == ClaimTransformationPosition.LinkExternalUser)
                {
                    SetSecret(trackLinkUpParty.LinkExternalUser.ClaimTransforms, claimTransform);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (mParty is OAuthDownParty)
            {
                var oauthDownParty = await tenantDataRepository.GetAsync<OAuthDownParty>(mParty.Id);
                SetSecret(oauthDownParty.ClaimTransforms, claimTransform);
            }
            else if (mParty is OidcDownParty)
            {
                var oidcDownParty = await tenantDataRepository.GetAsync<OidcDownParty>(mParty.Id);
                SetSecret(oidcDownParty.ClaimTransforms, claimTransform);
            }
            else if (mParty is SamlDownParty)
            {
                var samlDownParty = await tenantDataRepository.GetAsync<SamlDownParty>(mParty.Id);
                SetSecret(samlDownParty.ClaimTransforms, claimTransform);
            }
            else if (mParty is TrackLinkDownParty)
            {
                var trackLinkDownParty = await tenantDataRepository.GetAsync<TrackLinkDownParty>(mParty.Id);
                SetSecret(trackLinkDownParty.ClaimTransforms, claimTransform);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void SetSecret<MClaimTransformDatabase, MClaimTransform>(List<MClaimTransformDatabase> claimTransformsDatabase, MClaimTransform claimTransform) where MClaimTransformDatabase : ClaimTransform where MClaimTransform : ClaimTransform
        {
            claimTransform.Secret = claimTransformsDatabase.Where(c => c.Name == claimTransform.Name).Select(c => c.Secret).FirstOrDefault();
        }

        public bool ValidateModelUpPartyProfiles(ModelStateDictionary modelState, IEnumerable<UpPartyProfile> profiles)
        {
            var isValid = true;
            try
            {
                if (profiles?.Count() > 0)
                {
                    var duplicatedName = profiles.GroupBy(ct => ct.Name).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
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

        enum ClaimTransformationPosition
        {
            party,
            CreateUser,
            LinkExternalUser,
        }
    }
}
