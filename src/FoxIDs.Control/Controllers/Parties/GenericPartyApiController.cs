using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using System;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;
using ITfoxtec.Identity;
using FoxIDs.Models.Config;
using System.Collections.Generic;
using System.Linq;
using FoxIDs.Logic.Queues;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Abstract connection API.
    /// </summary>
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public abstract class GenericPartyApiController<AParty, AClaimTransform, MParty> : ApiController where AParty : Api.INewNameValue, Api.IClaimTransformRef<AClaimTransform> where MParty : Party where AClaimTransform : Api.ClaimTransform
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PartyLogic partyLogic;
        private readonly DownPartyCacheLogic downPartyCacheLogic;
        private readonly UpPartyCacheLogic upPartyCacheLogic;
        private readonly DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic;
        private readonly ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic;
        private readonly ValidateModelGenericPartyLogic validateModelGenericPartyLogic;

        public GenericPartyApiController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateApiModelGenericPartyLogic validateApiModelGenericPartyLogic, ValidateModelGenericPartyLogic validateModelGenericPartyLogic) : base(logger)
        {
            this.settings = settings;
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.partyLogic = partyLogic;
            this.downPartyCacheLogic = downPartyCacheLogic;
            this.upPartyCacheLogic = upPartyCacheLogic;
            this.downPartyAllowUpPartiesQueueLogic = downPartyAllowUpPartiesQueueLogic;
            this.validateApiModelGenericPartyLogic = validateApiModelGenericPartyLogic;
            this.validateModelGenericPartyLogic = validateModelGenericPartyLogic;
        }

        protected async Task<ActionResult<AParty>> Get(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                var mParty = await tenantDataRepository.GetAsync<MParty>(await GetId(name));
                return base.Ok(mapper.Map<AParty>(mParty));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(AParty).Name}' by name '{name}'.");
                    return NotFound(typeof(AParty).Name, name);
                }
                throw;
            }
        }

        protected async Task<ActionResult<AParty>> Post(AParty party, Func<AParty, ValueTask<bool>> apiModelActionAsync = null, Func<AParty, MParty, ValueTask<bool>> preLoadModelActionAsync = null, Func<AParty, MParty, ValueTask<bool>> postLoadModelActionAsync = null)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(party) || !validateApiModelGenericPartyLogic.ValidateApiModelClaimTransforms(ModelState, party.ClaimTransforms) || (apiModelActionAsync != null && !await apiModelActionAsync(party))) return BadRequest(ModelState);
                party.Name = await GetPartyNameAsync(party.Name);
                var profiles = GetUpPartyProfiles(party);
                if (profiles?.Count() > 0)
                {
                    foreach (var profile in profiles)
                    {
                        profile.Name = profile.Name.ToLower();
                    }
                }

                var mParty = mapper.Map<MParty>(party);
                if (mParty is UpParty)
                {
                    var count = await CountParties(Constants.Models.DataType.UpParty);
                    if (count >= Constants.Models.UpParty.PartiesMax)
                    {
                        throw new Exception($"Maximum number of authentication methods ({Constants.Models.UpParty.PartiesMax}) per environment has been reached.");
                    }
                }
                else if (mParty is DownParty)
                {
                    var count = await CountParties(Constants.Models.DataType.DownParty);
                    if (count >= Constants.Models.DownParty.PartiesMax)
                    {
                        throw new Exception($"Maximum number of application registrations ({Constants.Models.DownParty.PartiesMax}) per environment has been reached.");
                    }
                }
                else
                {
                    throw new NotSupportedException($"{mParty?.GetType()?.Name} type not supported.");
                }

                if (mParty is SamlDownParty samlDownParty && samlDownParty.Issuer.IsNullOrWhiteSpace())
                {
                    samlDownParty.Issuer = GetSamlIssuer(party.Name);
                }

                var mUpPartyProfiles = GetMUpPartyProfils(mParty);

                if (!(mParty is UpParty ? validateModelGenericPartyLogic.ValidateModelUpPartyProfiles(ModelState, mUpPartyProfiles) : true)) return BadRequest(ModelState);
                if (!(party is Api.IDownParty downParty ? await validateModelGenericPartyLogic.ValidateModelAllowUpPartiesAsync(ModelState, nameof(downParty.AllowUpParties), mParty as DownParty) : true)) return BadRequest(ModelState);
                if (!await validateModelGenericPartyLogic.ValidateModelClaimTransformsAsync(ModelState, mParty)) return BadRequest(ModelState);
                if (preLoadModelActionAsync != null && !await preLoadModelActionAsync(party, mParty)) return BadRequest(ModelState);
                if (postLoadModelActionAsync != null && !await postLoadModelActionAsync(party, mParty)) return BadRequest(ModelState);

                await tenantDataRepository.CreateAsync(mParty);

                return Created(mapper.Map<AParty>(mParty));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(AParty).Name}' by name '{party.Name}'.");
                    return Conflict(typeof(AParty).Name, party.Name, nameof(party.Name));
                }
                throw;
            }
        }

        protected async Task<ActionResult<AParty>> Put(AParty party, Func<AParty, ValueTask<bool>> apiModelActionAsync = null, Func<AParty, MParty, ValueTask<bool>> preLoadModelActionAsync = null, Func<AParty, MParty, ValueTask<bool>> postLoadModelActionAsync = null)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(party) || !validateApiModelGenericPartyLogic.ValidateApiModelClaimTransforms(ModelState, party.ClaimTransforms) || (apiModelActionAsync != null && !await apiModelActionAsync(party))) return BadRequest(ModelState);
                party.Name = party.Name?.ToLower();
                party.NewName = party.NewName?.ToLower();

                if(IsUpParty() && party.Name == Constants.DefaultLogin.Name && !party.NewName.IsNullOrEmpty())
                {
                    throw new Exception("The name can not be changed on the default Login authentication method.");
                }

                var upPartyProfiles = GetUpPartyProfiles(party);
                if (upPartyProfiles?.Count() > 0)
                {
                    foreach (var profile in upPartyProfiles)
                    {
                        profile.Name = profile.Name.ToLower();
                        profile.NewName = profile.NewName?.ToLower();
                    }
                }

                var mParty = mapper.Map<MParty>(party);

                if (!party.NewName.IsNullOrWhiteSpace())
                {
                    mParty.Name = party.NewName;
                    mParty.Id = await GetId(party.NewName);
                }
                var mUpPartyProfiles = GetMUpPartyProfils(mParty);
                if (mUpPartyProfiles?.Count() > 0)
                {
                    foreach (var mProfile in mUpPartyProfiles)
                    {
                        var profile = upPartyProfiles?.Where(p => !p.NewName.IsNullOrWhiteSpace() && p.Name == mProfile.Name).FirstOrDefault();
                        if (profile != null)
                        {
                            mProfile.Name = profile.NewName;
                        }
                    }
                }

                if(mParty is UpParty upParty)
                {
                    await validateModelGenericPartyLogic.HandleModelExtendedUiSecretAsync<AParty, AClaimTransform>(party, upParty);
                }
                await validateModelGenericPartyLogic.HandleClaimTransformationSecretAsync<AParty, AClaimTransform, MParty>(party, mParty);

                if (!(mParty is UpParty ? validateModelGenericPartyLogic.ValidateModelUpPartyProfiles(ModelState, mUpPartyProfiles) : true)) return BadRequest(ModelState);
                if (!(party is Api.IDownParty downParty ? await validateModelGenericPartyLogic.ValidateModelAllowUpPartiesAsync(ModelState, nameof(downParty.AllowUpParties), mParty as DownParty) : true)) return BadRequest(ModelState);
                if (!await validateModelGenericPartyLogic.ValidateModelClaimTransformsAsync(ModelState, mParty)) return BadRequest(ModelState);
                if (preLoadModelActionAsync != null && !await preLoadModelActionAsync(party, mParty)) return BadRequest(ModelState);

                if (mParty is OidcDownParty mOidcDownParty)
                {
                    var tempMParty = await tenantDataRepository.GetAsync<OidcDownParty>(await GetId(party.Name));
                    if(tempMParty.Client != null && mOidcDownParty.Client != null)
                    {
                        mOidcDownParty.Client.Secrets = tempMParty.Client.Secrets;
                    }

                    if(tempMParty.IsTest == true)
                    {
                        mOidcDownParty.IsTest = true;
                        mOidcDownParty.TestUrl = tempMParty.TestUrl;
                        if (!party.NewName.IsNullOrWhiteSpace())
                        {
                            mOidcDownParty.TestUrl = mOidcDownParty.TestUrl.Replace(party.Name, party.NewName);
                        }

                        var downPartyTestLifetime = party is Api.OidcDownParty aOidcDownParty && aOidcDownParty?.TestExpireInSeconds != null ? aOidcDownParty.TestExpireInSeconds.Value : settings.DownPartyTestLifetime;
                        if (downPartyTestLifetime > 0)
                        {
                            mOidcDownParty.TestExpireInSeconds = downPartyTestLifetime;
                            var newTestExpireAt = DateTimeOffset.UtcNow.AddSeconds(downPartyTestLifetime).ToUnixTimeSeconds();
                            if (newTestExpireAt > tempMParty.TestExpireAt)
                            {
                                mOidcDownParty.TestExpireAt = newTestExpireAt;
                            }
                            else
                            {
                                mOidcDownParty.TestExpireAt = tempMParty.TestExpireAt;
                            }
                        }
                        else
                        {
                            mOidcDownParty.TestExpireInSeconds = 0;
                            mOidcDownParty.TestExpireAt = -1;
                        }

                        mOidcDownParty.CodeVerifier = tempMParty.CodeVerifier;
                        mOidcDownParty.Nonce = tempMParty.Nonce;
                    }
                }
                else if (mParty is OAuthDownParty mOAuthDownParty)
                {
                    var tempMParty = await tenantDataRepository.GetAsync<OAuthDownParty>(await GetId(party.Name));
                    if (tempMParty.Client != null && mOAuthDownParty.Client != null)
                    {
                        mOAuthDownParty.Client.Secrets = tempMParty.Client.Secrets;
                    }
                }
                else if (mParty is OidcUpParty mOidcUpParty)
                {
                    var tempMOidcUpPartyParty = await tenantDataRepository.GetAsync<OidcUpParty>(await GetId(party.Name));
                    mOidcUpParty.Client.ClientSecret = tempMOidcUpPartyParty.Client.ClientSecret;
                    mOidcUpParty.Client.ClientKeys = tempMOidcUpPartyParty.Client.ClientKeys;
                }
                else if (mParty is ExternalLoginUpParty mExtLoginUpParty)
                {
                    var tempMExtLoginParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(await GetId(party.Name));
                    mExtLoginUpParty.Secret = tempMExtLoginParty.Secret;
                }

                if (mParty is SamlDownParty samlDownParty && samlDownParty.Issuer.IsNullOrWhiteSpace())
                {
                    samlDownParty.Issuer = GetSamlIssuer(party.Name);
                }

                if (postLoadModelActionAsync != null && !await postLoadModelActionAsync(party, mParty)) return BadRequest(ModelState);

                var oldMUpParty = (mParty is UpParty mUpParty) ? await tenantDataRepository.GetAsync<UpPartyWithProfile<UpPartyProfile>>(await GetId(party.Name)) : null;
               
                if (!party.NewName.IsNullOrWhiteSpace())
                {
                    await tenantDataRepository.CreateAsync(mParty);
                    await tenantDataRepository.DeleteAsync<MParty>(await GetId(party.Name));
                }
                else
                {
                    await tenantDataRepository.UpdateAsync(mParty);
                }

                if (mParty is UpParty)
                {                    
                    await upPartyCacheLogic.InvalidateUpPartyCacheAsync(party.Name);
                    await downPartyAllowUpPartiesQueueLogic.UpdateUpParty(oldMUpParty, mParty as UpParty, upPartyProfiles);
                }
                else if (mParty is DownParty)
                {
                    await downPartyCacheLogic.InvalidateDownPartyCacheAsync(party.Name);
                }
                else
                {
                    throw new NotSupportedException($"{mParty?.GetType()?.Name} type not supported.");
                }

                return Ok(mapper.Map<AParty>(mParty));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(AParty).Name}' by name '{party.Name}'.");
                    return NotFound(typeof(AParty).Name, party.Name, nameof(party.Name));
                }
                throw;
            }
        }

        private IEnumerable<Api.IProfile> GetUpPartyProfiles(AParty party)
        {
            if(party is Api.OidcUpParty oidcUpParty)
            {
                return oidcUpParty.Profiles;
            }
            else if (party is Api.SamlUpParty samlUpParty)
            {
                return samlUpParty.Profiles;
            }
            else if (party is Api.TrackLinkUpParty trackLinkUpParty)
            {
                return trackLinkUpParty.Profiles;
            }
            else if(party is Api.ExternalLoginUpParty externalLoginUpParty)
            {
                return externalLoginUpParty.Profiles;
            }
            else
            {
                return null;
            }
        }

        private IEnumerable<UpPartyProfile> GetMUpPartyProfils(MParty mParty)
        {
            if (mParty is OidcUpParty oidcUpParty && oidcUpParty.Profiles != null)
            {
                return oidcUpParty.Profiles.Cast<UpPartyProfile>();
            }
            else if (mParty is SamlUpParty samlUpParty && samlUpParty.Profiles != null)
            {
                return samlUpParty.Profiles.Cast<UpPartyProfile>();
            }
            else if (mParty is TrackLinkUpParty trackLinkUpParty && trackLinkUpParty.Profiles != null)
            {
                return trackLinkUpParty.Profiles.Cast<UpPartyProfile>();
            }
            else if (mParty is ExternalLoginUpParty externalLoginUpParty && externalLoginUpParty.Profiles != null)
            {
                return externalLoginUpParty.Profiles.Cast<UpPartyProfile>();
            }

            return null;
        }

        protected async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                if (Constants.DefaultLogin.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"The default login with the name '{Constants.DefaultLogin.Name}' can not be deleted.");
                }

                await tenantDataRepository.DeleteAsync<MParty>(await GetId(name));

                if (IsUpParty())
                {
                    await upPartyCacheLogic.InvalidateUpPartyCacheAsync(name);
                    await downPartyAllowUpPartiesQueueLogic.DeleteUpParty(name);
                }
                else
                {
                    await downPartyCacheLogic.InvalidateDownPartyCacheAsync(name);
                }

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(AParty).Name}' by id '{name}'.");
                    return NotFound(typeof(AParty).Name, name);
                }
                throw;
            }
        }

        private Task<string> GetId(string name)
        {
            if (IsUpParty())
            {
                return UpParty.IdFormatAsync(RouteBinding, name);
            }
            else
            {
                return DownParty.IdFormatAsync(RouteBinding, name);
            }
        }

        private bool IsUpParty()
        {
            if (EqualsBaseType(0, typeof(MParty), typeof(UpParty)))
            {
                return true;
            }
            else if (EqualsBaseType(0, typeof(MParty), typeof(DownParty)))
            {
                return false;
            }
            else
            {
                throw new NotSupportedException($"{typeof(MParty)} type not supported.");
            }
        }

        private bool EqualsBaseType(int recursivCount, Type type, Type baseType)
        {
            var bt = type.BaseType;
            if (bt.Equals(baseType)) return true;

            if (recursivCount > 3) return false;

            recursivCount++;
            return EqualsBaseType(recursivCount, bt, baseType);
        }

        private async Task<long> CountParties(string dataType)
        {
            return await tenantDataRepository.CountAsync<Party>(new Party.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName }, whereQuery: p => p.DataType.Equals(dataType));
        }

        private async Task<string> GetPartyNameAsync(string name)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return await partyLogic.GeneratePartyNameAsync(IsUpParty());
            }
            else
            {
                return name.ToLower();
            }
        }

        private string GetSamlIssuer(string name)
        {
            return $"uri:{name}";
        }
    }
}
