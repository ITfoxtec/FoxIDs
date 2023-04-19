using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using System;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Abstract party API.
    /// </summary>
    public abstract class GenericPartyApiController<AParty, AClaimTransform, MParty> : TenantApiController where AParty : Api.INameValue, Api.IClaimTransform<AClaimTransform> where MParty : Party where AClaimTransform : Api.ClaimTransform
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly DownPartyCacheLogic downPartyCacheLogic;
        private readonly UpPartyCacheLogic upPartyCacheLogic;
        private readonly DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic;
        private readonly ValidateGenericPartyLogic validateGenericPartyLogic;

        public GenericPartyApiController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, DownPartyCacheLogic downPartyCacheLogic, UpPartyCacheLogic upPartyCacheLogic, DownPartyAllowUpPartiesQueueLogic downPartyAllowUpPartiesQueueLogic, ValidateGenericPartyLogic validateGenericPartyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.downPartyCacheLogic = downPartyCacheLogic;
            this.upPartyCacheLogic = upPartyCacheLogic;
            this.downPartyAllowUpPartiesQueueLogic = downPartyAllowUpPartiesQueueLogic;
            this.validateGenericPartyLogic = validateGenericPartyLogic;
        }

        protected async Task<ActionResult<AParty>> Get(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                var MParty = await tenantRepository.GetAsync<MParty>(await GetId(IsUpParty(), name));
                return Ok(mapper.Map<AParty>(MParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(AParty).Name}' by name '{name}'.");
                    return NotFound(typeof(AParty).Name, name);
                }
                throw;
            }
        }

        protected async Task<ActionResult<AParty>> Post(AParty party, Func<AParty, ValueTask<bool>> apiModelActionAsync = null, Func<AParty, MParty, ValueTask<bool>> modelActionAsync = null)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(party) || !validateGenericPartyLogic.ValidateApiModelClaimTransforms(ModelState, party.ClaimTransforms) || (apiModelActionAsync != null &&!await apiModelActionAsync(party))) return BadRequest(ModelState);

                var mParty = mapper.Map<MParty>(party);
                if (!(party is Api.IDownParty downParty ? await validateGenericPartyLogic.ValidateModelAllowUpPartiesAsync(ModelState, nameof(downParty.AllowUpPartyNames), mParty as DownParty) : true)) return BadRequest(ModelState);
                if (!validateGenericPartyLogic.ValidateModelClaimTransforms(ModelState, mParty)) return BadRequest(ModelState);
                if (modelActionAsync != null && !await modelActionAsync(party, mParty)) return BadRequest(ModelState);

                await tenantRepository.CreateAsync(mParty);

                if (mParty is UpParty)
                {
                    await upPartyCacheLogic.InvalidateUpPartyCacheAsync(party.Name);
                    await downPartyAllowUpPartiesQueueLogic.UpdateUpParty(null, mParty as UpParty);
                }
                else if (mParty is DownParty)
                {
                    await downPartyCacheLogic.InvalidateDownPartyCacheAsync(party.Name);
                }
                else
                {
                    throw new NotSupportedException($"{mParty?.GetType()?.Name} type not supported.");
                }

                return Created(mapper.Map<AParty>(mParty));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(AParty).Name}' by name '{party.Name}'.");
                    return Conflict(typeof(AParty).Name, party.Name, nameof(party.Name));
                }
                throw;
            }
        }

        protected async Task<ActionResult<AParty>> Put(AParty party, Func<AParty, ValueTask<bool>> apiModelActionAsync = null, Func<AParty, MParty, ValueTask<bool>> modelActionAsync = null)
        {
            try
            {
               if (!await ModelState.TryValidateObjectAsync(party) || !validateGenericPartyLogic.ValidateApiModelClaimTransforms(ModelState, party.ClaimTransforms) || (apiModelActionAsync != null && !await apiModelActionAsync(party))) return BadRequest(ModelState);

                var mParty = mapper.Map<MParty>(party);
                if (!(party is Api.IDownParty downParty ? await validateGenericPartyLogic.ValidateModelAllowUpPartiesAsync(ModelState, nameof(downParty.AllowUpPartyNames), mParty as DownParty) : true)) return BadRequest(ModelState);
                if (!validateGenericPartyLogic.ValidateModelClaimTransforms(ModelState, mParty)) return BadRequest(ModelState);
                if (modelActionAsync != null && !await modelActionAsync(party, mParty)) return BadRequest(ModelState);

                if(party is Api.OidcDownParty)
                {
                    var tempMParty = await tenantRepository.GetAsync<MParty>(mParty.Id);
                    if((tempMParty as OidcDownParty)?.Client?.Secrets?.Count > 0)
                    {
                        (mParty as OidcDownParty).Client.Secrets = (tempMParty as OidcDownParty).Client.Secrets;
                    }
                }
                else if (party is Api.OAuthDownParty)
                {
                    var tempMParty = await tenantRepository.GetAsync<MParty>(mParty.Id);
                    if ((tempMParty as OAuthDownParty)?.Client?.Secrets?.Count > 0)
                    {
                        (mParty as OAuthDownParty).Client.Secrets = (tempMParty as OAuthDownParty).Client.Secrets;
                    }
                }

                var oldMUpParty = (mParty is UpParty) ? await tenantRepository.GetAsync<UpParty>(await UpParty.IdFormatAsync(RouteBinding, mParty.Name)) : null;      
                await tenantRepository.UpdateAsync(mParty);

                if (mParty is UpParty)
                {
                    await upPartyCacheLogic.InvalidateUpPartyCacheAsync(party.Name);
                    await downPartyAllowUpPartiesQueueLogic.UpdateUpParty(oldMUpParty, mParty as UpParty);
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
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(AParty).Name}' by name '{party.Name}'.");
                    return NotFound(typeof(AParty).Name, party.Name, nameof(party.Name));
                }
                throw;
            }
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

                var isUpParty = IsUpParty();
                await tenantRepository.DeleteAsync<MParty>(await GetId(isUpParty, name));

                if (isUpParty)
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
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(AParty).Name}' by id '{name}'.");
                    return NotFound(typeof(AParty).Name, name);
                }
                throw;
            }
        }

        private bool IsUpParty()
        {
            if (EqualsBaseType(0, typeof(MParty), (typeof(UpParty))))
            {
                return true;
            }
            else if (EqualsBaseType(0, typeof(MParty), (typeof(DownParty))))
            {
                return false;
            }
            else
            {
                throw new NotSupportedException($"{typeof(MParty)} type not supported.");
            }
        }

        private Task<string> GetId(bool isUpParty, string name)
        {
            if(isUpParty)
            {
                return UpParty.IdFormatAsync(RouteBinding, name);
            }
            else 
            {
                return DownParty.IdFormatAsync(RouteBinding, name);
            }
        }

        private bool EqualsBaseType(int recursivCount, Type type, Type baseType)
        {
            var bt = type.BaseType;
            if (bt.Equals(baseType)) return true;

            if (recursivCount > 2) return false;

            recursivCount++;
            return EqualsBaseType(recursivCount, bt, baseType);
        }
    }
}
