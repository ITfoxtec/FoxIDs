using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
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
                       await ValidateModelClaimTransformsAsync(modelState, mParty, loginUpParty.ExitClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, loginUpParty.CreateUser?.ClaimTransforms);
            }
            else if (mParty is ExternalLoginUpParty externalLoginUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, externalLoginUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, externalLoginUpParty.ExitClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, externalLoginUpParty.LinkExternalUser?.ClaimTransforms);
            }
            else if (mParty is OAuthUpParty oauthUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, oauthUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, oauthUpParty.ExitClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, oauthUpParty.LinkExternalUser?.ClaimTransforms);
            }
            else if (mParty is OidcUpParty oidchUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, oidchUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, oidchUpParty.ExitClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, oidchUpParty.LinkExternalUser?.ClaimTransforms);
            }
            else if (mParty is SamlUpParty samlUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, samlUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, samlUpParty.ExitClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, samlUpParty.LinkExternalUser?.ClaimTransforms);
            }
            else if (mParty is TrackLinkUpParty trackLinkUpParty)
            {
                return await ValidateModelClaimTransformsAsync(modelState, mParty, trackLinkUpParty.ClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, trackLinkUpParty.ExitClaimTransforms) &&
                       await ValidateModelClaimTransformsAsync(modelState, mParty, trackLinkUpParty.LinkExternalUser?.ClaimTransforms);
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

        private async Task<bool> ValidateModelClaimTransformsAsync<MParty, MClaimTransform>(ModelStateDictionary modelState, MParty mParty, List<MClaimTransform> claimTransforms) where MParty : Party where MClaimTransform : ClaimTransform
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

        public async Task HandleClaimTransformationSecretAsync<AParty, AClaimTransform, MParty>(AParty party, MParty mParty) 
            where AParty : Api.INewNameValue, Api.IClaimTransformRef<AClaimTransform> where AClaimTransform : Api.ClaimTransform 
            where MParty : Party
        {
            if (mParty is LoginUpParty mLoginUpParty)
            {
                await HandleClaimTransformationSecretLoginUpPartyAsync(party as Api.LoginUpParty, mLoginUpParty);
            }
            else if (mParty is ExternalLoginUpParty mExternalLoginUpParty)
            {
                await HandleClaimTransformationSecretUpPartyAsync(party as Api.ExternalLoginUpParty, mExternalLoginUpParty);
            }
            else if (mParty is OAuthUpParty oauthUpParty)
            {
                await HandleClaimTransformationSecretUpPartyOnlyOAuthAsync(party as Api.OAuthUpParty, oauthUpParty);
            }
            else if (mParty is OidcUpParty oidchUpParty)
            {
                await HandleClaimTransformationSecretUpPartyAsync(party as Api.OidcUpParty, oidchUpParty);
            }
            else if (mParty is SamlUpParty samlUpParty)
            {
                await HandleClaimTransformationSecretSamlUpPartyAsync(party as Api.SamlUpParty, samlUpParty);
            }
            else if (mParty is TrackLinkUpParty trackLinkUpParty)
            {
                await HandleClaimTransformationSecretUpPartyAsync(party as Api.TrackLinkUpParty, trackLinkUpParty);
            }
            else if (mParty is OAuthDownParty oauthDownParty)
            {
                await HandleClaimTransformationSecretDownPartyAsync(party as Api.OAuthDownParty, oauthDownParty);
            }
            else if (mParty is OidcDownParty oidcDownParty)
            {
                await HandleClaimTransformationSecretDownPartyAsync(party as Api.OidcDownParty, oidcDownParty);
            }
            else if (mParty is SamlDownParty samlDownParty)
            {
                await HandleClaimTransformationSecretSamlDownPartyAsync(party as Api.SamlDownParty, samlDownParty);
            }
            else if (mParty is TrackLinkDownParty trackLinkDownParty)
            {
                await HandleClaimTransformationSecretDownPartyAsync(party as Api.TrackLinkDownParty, trackLinkDownParty);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private async Task HandleClaimTransformationSecretLoginUpPartyAsync<AUpParty, MUpParty>(AUpParty aUpParty, MUpParty mUpParty) 
            where AUpParty : Api.INameValue, Api.IClaimTransformRef<Api.OAuthClaimTransform>, Api.ICreateUserRef, Api.IExitClaimTransformsRef<Api.OAuthClaimTransform>
            where MUpParty : UpParty, IOAuthClaimTransformsRef, ICreateUserRef
        {
            var apiClaimTransforms = aUpParty.ClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            var apiExitClaimTransforms = aUpParty.ExitClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            var apiCreateUserClaimTransforms = aUpParty.CreateUser?.ClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            if (apiClaimTransforms?.Any() == true || apiExitClaimTransforms?.Any() == true || apiCreateUserClaimTransforms?.Any() == true)
            {
                var dbUpParty = await tenantDataRepository.GetAsync<MUpParty>(mUpParty.Id);

                if (apiClaimTransforms?.Any() == true)
                {
                    foreach (var mClaimTransforms in mUpParty.ClaimTransforms.Where(c => apiClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mClaimTransforms.Secret = dbUpParty.ClaimTransforms?.Where(u => u.Name == mClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
                if (apiExitClaimTransforms?.Any() == true)
                {
                    foreach (var mExitClaimTransforms in mUpParty.ExitClaimTransforms.Where(c => apiExitClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mExitClaimTransforms.Secret = dbUpParty.ExitClaimTransforms?.Where(u => u.Name == mExitClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
                if (apiCreateUserClaimTransforms?.Any() == true)
                {
                    foreach (var mCreateUserClaimTransforms in mUpParty.CreateUser.ClaimTransforms.Where(c => apiCreateUserClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mCreateUserClaimTransforms.Secret = dbUpParty.CreateUser?.ClaimTransforms?.Where(u => u.Name == mCreateUserClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
            }
        }

        private async Task HandleClaimTransformationSecretUpPartyAsync<AUpParty, MUpParty>(AUpParty aUpParty, MUpParty mUpParty) 
            where AUpParty : Api.INameValue, Api.IClaimTransformRef<Api.OAuthClaimTransform>, Api.ILinkExternalUserRef, Api.IExitClaimTransformsRef<Api.OAuthClaimTransform>
            where MUpParty : UpParty, IOAuthClaimTransformsRef, ILinkExternalUserRef
        {
            var apiClaimTransforms = aUpParty.ClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            var apiExitClaimTransforms = aUpParty.ExitClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            var apiLinkExternalUserClaimTransforms = aUpParty.LinkExternalUser?.ClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            if (apiClaimTransforms?.Any() == true || apiExitClaimTransforms?.Any() == true || apiLinkExternalUserClaimTransforms?.Any() == true)
            {
                var dbUpParty = await tenantDataRepository.GetAsync<MUpParty>(mUpParty.Id);

                if (apiClaimTransforms?.Any() == true)
                {
                    foreach (var mClaimTransforms in mUpParty.ClaimTransforms.Where(c => apiClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mClaimTransforms.Secret = dbUpParty.ClaimTransforms?.Where(u => u.Name == mClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
                if (apiExitClaimTransforms?.Any() == true)
                {
                    foreach (var mExitClaimTransforms in mUpParty.ExitClaimTransforms.Where(c => apiExitClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mExitClaimTransforms.Secret = dbUpParty.ExitClaimTransforms?.Where(u => u.Name == mExitClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
                if (apiLinkExternalUserClaimTransforms?.Any() == true)
                {
                    foreach (var mLinkExternalUserClaimTransforms in mUpParty.LinkExternalUser.ClaimTransforms.Where(c => apiLinkExternalUserClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mLinkExternalUserClaimTransforms.Secret = dbUpParty.LinkExternalUser?.ClaimTransforms?.Where(u => u.Name == mLinkExternalUserClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
            }
        }

        private async Task HandleClaimTransformationSecretUpPartyOnlyOAuthAsync<AUpParty, MUpParty>(AUpParty aUpParty, MUpParty mUpParty)
            where AUpParty : Api.INameValue, Api.IClaimTransformRef<Api.OAuthClaimTransform>
            where MUpParty : UpParty, IOAuthClaimTransformsRef, ILinkExternalUserRef
        {
            var apiClaimTransforms = aUpParty.ClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            if (apiClaimTransforms?.Any() == true)
            {
                var dbUpParty = await tenantDataRepository.GetAsync<MUpParty>(mUpParty.Id);

                if (apiClaimTransforms?.Any() == true)
                {
                    foreach (var mClaimTransforms in mUpParty.ClaimTransforms.Where(c => apiClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mClaimTransforms.Secret = dbUpParty.ClaimTransforms?.Where(u => u.Name == mClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
            }
        }

        private async Task HandleClaimTransformationSecretSamlUpPartyAsync<AUpParty, MUpParty>(AUpParty aUpParty, MUpParty mUpParty)
          where AUpParty : Api.INameValue, Api.IClaimTransformRef<Api.SamlClaimTransform>, Api.ILinkExternalUserRef, Api.IExitClaimTransformsRef<Api.OAuthClaimTransform>
          where MUpParty : UpParty, ISamlClaimTransformsRef, ILinkExternalUserRef
        {
            var apiClaimTransforms = aUpParty.ClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            var apiExitClaimTransforms = aUpParty.ExitClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            var apiLinkExternalUserClaimTransforms = aUpParty.LinkExternalUser?.ClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            if (apiClaimTransforms?.Any() == true || apiExitClaimTransforms?.Any() == true || apiLinkExternalUserClaimTransforms?.Any() == true)
            {
                var dbUpParty = await tenantDataRepository.GetAsync<MUpParty>(mUpParty.Id);

                if (apiClaimTransforms?.Any() == true)
                {
                    foreach (var mClaimTransforms in mUpParty.ClaimTransforms.Where(c => apiClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mClaimTransforms.Secret = dbUpParty.ClaimTransforms?.Where(u => u.Name == mClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
                if (apiExitClaimTransforms?.Any() == true)
                {
                    foreach (var mExitClaimTransforms in mUpParty.ExitClaimTransforms.Where(c => apiExitClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mExitClaimTransforms.Secret = dbUpParty.ExitClaimTransforms?.Where(u => u.Name == mExitClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
                if (apiLinkExternalUserClaimTransforms?.Any() == true)
                {
                    foreach (var mLinkExternalUserClaimTransforms in mUpParty.LinkExternalUser.ClaimTransforms.Where(c => apiLinkExternalUserClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mLinkExternalUserClaimTransforms.Secret = dbUpParty.LinkExternalUser?.ClaimTransforms?.Where(u => u.Name == mLinkExternalUserClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
            }
        }

        private async Task HandleClaimTransformationSecretDownPartyAsync<ADownParty, MDownParty>(ADownParty aDownParty, MDownParty mDownParty)
            where ADownParty : Api.INameValue, Api.IClaimTransformRef<Api.OAuthClaimTransform>
            where MDownParty : DownParty, IOAuthClaimTransformsRef
        {
            var apiClaimTransforms = aDownParty.ClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            if (apiClaimTransforms?.Any() == true)
            {
                var dbUpParty = await tenantDataRepository.GetAsync<MDownParty>(mDownParty.Id);

                if (apiClaimTransforms?.Any() == true)
                {
                    foreach (var mClaimTransforms in mDownParty.ClaimTransforms.Where(c => apiClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mClaimTransforms.Secret = dbUpParty.ClaimTransforms?.Where(u => u.Name == mClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
            }
        }

        private async Task HandleClaimTransformationSecretSamlDownPartyAsync<ADownParty, MDownParty>(ADownParty aDownParty, MDownParty mDownParty)
            where ADownParty : Api.INameValue, Api.IClaimTransformRef<Api.SamlClaimTransform>
            where MDownParty : DownParty, ISamlClaimTransformsRef
        {
            var apiClaimTransforms = aDownParty.ClaimTransforms?.Where(c => c.ExternalConnectType == Api.ExternalConnectTypes.Api && c.Secret == c.SecretLoaded);
            if (apiClaimTransforms?.Any() == true)
            {
                var dbUpParty = await tenantDataRepository.GetAsync<MDownParty>(mDownParty.Id);

                if (apiClaimTransforms?.Any() == true)
                {
                    foreach (var mClaimTransforms in mDownParty.ClaimTransforms.Where(c => apiClaimTransforms.Any(ac => ac.Name == c.Name)))
                    {
                        mClaimTransforms.Secret = dbUpParty.ClaimTransforms?.Where(u => u.Name == mClaimTransforms.Name).Select(c => c.Secret).FirstOrDefault();
                    }
                }
            }
        }

        public async Task HandleModelExtendedUiSecretAsync<AParty, AClaimTransform>(AParty aParty, UpParty mParty) 
            where AParty : Api.INewNameValue, Api.IClaimTransformRef<AClaimTransform> where AClaimTransform : Api.ClaimTransform
        {
            if (mParty.ExtendedUis?.Where(u => u.ExternalConnectType == ExternalConnectTypes.Api)?.Count() > 0)
            {
                if (mParty is LoginUpParty)
                {
                    await SetExtendedUiSecretAsync(aParty as Api.LoginUpParty, mParty);
                }
                else if (mParty is ExternalLoginUpParty)
                {
                    await SetExtendedUiSecretAsync(aParty as Api.ExternalLoginUpParty, mParty);
                }
                else if (mParty is OidcUpParty)
                {
                    await SetExtendedUiSecretAsync(aParty as Api.OidcUpParty, mParty);
                }
                else if (mParty is SamlUpParty)
                {
                    await SetExtendedUiSecretAsync(aParty as Api.SamlUpParty, mParty);
                }
                else if (mParty is TrackLinkUpParty)
                {
                    await SetExtendedUiSecretAsync(aParty as Api.TrackLinkUpParty, mParty);
                }
            }
        }

        private async Task SetExtendedUiSecretAsync<AUpParty, MUpParty>(AUpParty aUpParty, MUpParty mParty)
            where AUpParty : Api.IExtendedUisRef
            where MUpParty : UpParty
        {
            var apiPartyExtendedUis = aUpParty.ExtendedUis.Where(u => u.ExternalConnectType == Api.ExternalConnectTypes.Api && u.Secret == u.SecretLoaded);
            if (apiPartyExtendedUis?.Any() == true)
            {
                var dbUpParty = await tenantDataRepository.GetAsync<MUpParty>(mParty.Id);
                foreach (var mExtendedUi in mParty.ExtendedUis.Where(u => apiPartyExtendedUis.Any(au => au.Name == u.Name)))
                {
                    mExtendedUi.Secret = dbUpParty.ExtendedUis?.Where(u => u.Name == mExtendedUi.Name).Select(c => c.Secret).FirstOrDefault();
                }
            }
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
    }
}
