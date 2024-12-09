using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Infrastructure.Security;
using System;
using FoxIDs.Logic;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TExternalUserController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly ValidateApiModelExternalUserLogic validateApiModelExternalUserLogic;

        public TExternalUserController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, ValidateApiModelExternalUserLogic validateApiModelExternalUserLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.validateApiModelExternalUserLogic = validateApiModelExternalUserLogic;
        }

        /// <summary>
        /// Get external user.
        /// </summary>
        /// <returns>External user.</returns>
        [ProducesResponseType(typeof(Api.ExternalUser), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ExternalUser>> GetExternalUser(Api.ExternalUserId userRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(userRequest)) return BadRequest(ModelState);

                var mExternalUser = await tenantDataRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, await GetLinkClaimHashAsync(userRequest.LinkClaimValue, userRequest.RedemptionClaimValue)));
                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.ExternalUser).Name}' by up-party name '{userRequest.UpPartyName}' and link claim '{userRequest.LinkClaimValue}'.");
                    return NotFound(typeof(Api.ExternalUser).Name, $"{userRequest.UpPartyName}:{userRequest.LinkClaimValue}");
                }
                throw;
            }
        }

        /// <summary>
        /// Create external user.
        /// </summary>
        /// <param name="userRequest">ExternalUser.</param>
        /// <returns>ExternalUser.</returns>
        [ProducesResponseType(typeof(Api.ExternalUser), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.ExternalUser>> PostExternalUser([FromBody] Api.ExternalUserRequest userRequest)
        {

            try
            {
                if (!await ModelState.TryValidateObjectAsync(userRequest) || !await validateApiModelExternalUserLogic.ValidateApiModelAsync(ModelState, userRequest)) return BadRequest(ModelState);

                var mExternalUser = mapper.Map<ExternalUser>(userRequest);
                mExternalUser.Id = await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, await GetLinkClaimHashAsync(userRequest.LinkClaimValue, userRequest.RedemptionClaimValue));
                mExternalUser.UserId = Guid.NewGuid().ToString();
                await tenantDataRepository.CreateAsync(mExternalUser);

                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.ExternalUserId).Name}' by up-party name '{userRequest.UpPartyName}' and link claim '{userRequest.LinkClaimValue}'.");
                    return Conflict(typeof(Api.ExternalUserId).Name, $"{userRequest.UpPartyName}:{userRequest.LinkClaimValue}");
                }
                throw;
            }
        }

        /// <summary>
        /// Update external user.
        /// </summary>
        /// <param name="userRequest">External user.</param>
        /// <returns>External user.</returns>
        [ProducesResponseType(typeof(Api.ExternalUser), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ExternalUser>> PutExternalUser([FromBody] Api.ExternalUserUpdateRequest userRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(userRequest)) return BadRequest(ModelState);

                var mExternalUser = await tenantDataRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, await GetLinkClaimHashAsync(userRequest.LinkClaimValue, userRequest.RedemptionClaimValue)));

                mExternalUser.LinkClaimValue = userRequest.UpdateLinkClaimValue;
                mExternalUser.RedemptionClaimValue = userRequest.UpdateRedemptionClaimValue;
                mExternalUser.DisableAccount = userRequest.DisableAccount;
                var tempMExternalUser = mapper.Map<ExternalUser>(userRequest);
                mExternalUser.Claims = tempMExternalUser.Claims;

                if (!userRequest.LinkClaimValue.IsNullOrWhiteSpace() && userRequest.LinkClaimValue != userRequest.UpdateLinkClaimValue ||             // if link claim change
                    userRequest.LinkClaimValue.IsNullOrWhiteSpace() && !userRequest.UpdateLinkClaimValue.IsNullOrWhiteSpace() ||                      // if link claim is added
                    userRequest.LinkClaimValue.IsNullOrWhiteSpace() && userRequest.RedemptionClaimValue != userRequest.UpdateRedemptionClaimValue)    // if link claim not set and redemption claim change
                {
                    await tenantDataRepository.DeleteAsync<ExternalUser>(mExternalUser.Id);
                    mExternalUser.Id = await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, await GetLinkClaimHashAsync(mExternalUser.LinkClaimValue, mExternalUser.RedemptionClaimValue));
                    await tenantDataRepository.CreateAsync(mExternalUser);
                }
                else
                {
                    await tenantDataRepository.SaveAsync(mExternalUser);
                }

                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.ExternalUserId).Name}' by up-party name '{userRequest.UpPartyName}' and link claim '{userRequest.LinkClaimValue}'.");
                    return NotFound(typeof(Api.ExternalUserId).Name, $"{userRequest.UpPartyName}:{userRequest.LinkClaimValue}");
                }
                throw;
            }
        }

        /// <summary>
        /// Delete external user.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteExternalUser(Api.ExternalUserId userRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(userRequest)) return BadRequest(ModelState);

                await tenantDataRepository.DeleteAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, await GetLinkClaimHashAsync(userRequest.LinkClaimValue, userRequest.RedemptionClaimValue)));
                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.ExternalUserId).Name}' by up-party name '{userRequest.UpPartyName}' and link claim '{userRequest.LinkClaimValue}'.");
                    return NotFound(typeof(Api.ExternalUserId).Name, $"{userRequest.UpPartyName}:{userRequest.LinkClaimValue}");
                }
                throw;
            }
        }

        private Task<string> GetLinkClaimHashAsync(string linkClaimValue, string redemptionClaimValue)
        {
            if (linkClaimValue.IsNullOrWhiteSpace())
            {
                return redemptionClaimValue.HashIdStringAsync();
            }
            else
            {
                return linkClaimValue.HashIdStringAsync();
            }
        }
    }
}
