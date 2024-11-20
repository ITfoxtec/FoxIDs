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

                var linkClaimHash = await userRequest.LinkClaimValue.HashIdStringAsync();
                var mExternalUser = await tenantDataRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, linkClaimHash));
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
        /// <param name="externalUserRequest">ExternalUser.</param>
        /// <returns>ExternalUser.</returns>
        [ProducesResponseType(typeof(Api.ExternalUser), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.ExternalUser>> PostExternalUser([FromBody] Api.ExternalUserRequest externalUserRequest)
        {

            try
            {
                if (!await ModelState.TryValidateObjectAsync(externalUserRequest) || !await validateApiModelExternalUserLogic.ValidateApiModelAsync(ModelState, externalUserRequest)) return BadRequest(ModelState);

                var mExternalUser = mapper.Map<ExternalUser>(externalUserRequest);
                var linkClaimHash = await externalUserRequest.LinkClaimValue.HashIdStringAsync();
                mExternalUser.Id = await ExternalUser.IdFormatAsync(RouteBinding, externalUserRequest.UpPartyName, linkClaimHash);
                mExternalUser.UserId = Guid.NewGuid().ToString();
                await tenantDataRepository.CreateAsync(mExternalUser);

                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.ExternalUserId).Name}' by up-party name '{externalUserRequest.UpPartyName}' and link claim '{externalUserRequest.LinkClaimValue}'.");
                    return Conflict(typeof(Api.ExternalUserId).Name, $"{externalUserRequest.UpPartyName}:{externalUserRequest.LinkClaimValue}");
                }
                throw;
            }
        }

        /// <summary>
        /// Update external user.
        /// </summary>
        /// <param name="externalUserRequest">External user.</param>
        /// <returns>External user.</returns>
        [ProducesResponseType(typeof(Api.ExternalUser), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ExternalUser>> PutExternalUser([FromBody] Api.ExternalUserRequest externalUserRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(externalUserRequest)) return BadRequest(ModelState);

                var linkClaimHash = await externalUserRequest.LinkClaimValue.HashIdStringAsync();              
                var mExternalUser = await tenantDataRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, externalUserRequest.UpPartyName, linkClaimHash));

                var tempMExternalUser = mapper.Map<ExternalUser>(externalUserRequest);
                mExternalUser.DisableAccount = tempMExternalUser.DisableAccount;
                mExternalUser.Claims = tempMExternalUser.Claims;   
                await tenantDataRepository.SaveAsync(mExternalUser);

                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.ExternalUserId).Name}' by up-party name '{externalUserRequest.UpPartyName}' and link claim '{externalUserRequest.LinkClaimValue}'.");
                    return NotFound(typeof(Api.ExternalUserId).Name, $"{externalUserRequest.UpPartyName}:{externalUserRequest.LinkClaimValue}");
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

                var linkClaimHash = await userRequest.LinkClaimValue.HashIdStringAsync();
                await tenantDataRepository.DeleteAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, linkClaimHash));
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
    }
}
