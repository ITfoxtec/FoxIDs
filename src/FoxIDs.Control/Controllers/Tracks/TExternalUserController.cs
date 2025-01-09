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
using System.Linq;
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
                userRequest = ToLower(userRequest);

                var mExternalUser = await GetExternalUserAsync(userRequest);
                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.ExternalUser).Name}' by up-party name '{userRequest.UpPartyName}' and link claim '{userRequest.LinkClaimValue}' or redemption claim '{userRequest.RedemptionClaimValue}'.");
                    return NotFound(typeof(Api.ExternalUser).Name, $"{userRequest.UpPartyName}:{(!userRequest.LinkClaimValue.IsNullOrWhiteSpace() ? userRequest.LinkClaimValue : userRequest.RedemptionClaimValue)}");
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
                if (!await ModelState.TryValidateObjectAsync(userRequest)) return BadRequest(ModelState);
                userRequest = ToLower(userRequest);
                if (!await validateApiModelExternalUserLogic.ValidateApiModelAsync(ModelState, userRequest)) return BadRequest(ModelState);

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
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.ExternalUserId).Name}' by up-party name '{userRequest.UpPartyName}' and link claim '{userRequest.LinkClaimValue}' or redemption claim '{userRequest.RedemptionClaimValue}'.");
                    return Conflict(typeof(Api.ExternalUserId).Name, $"{userRequest.UpPartyName}:{(!userRequest.LinkClaimValue.IsNullOrWhiteSpace() ? userRequest.LinkClaimValue : userRequest.RedemptionClaimValue)}");
                }
                throw;
            }
        }

        /// <summary>
        /// Update external user.
        /// Add a value in the 'UpdateUpPartyName' attribute to change which authentication method (up-party) the external user is connected to.
        /// Add a value in the 'LinkClaimValue' and / or 'RedemptionClaimValue' attributes to change the link claim value and / or redemption claim value. The field is set to an empty string if the value is a empty string.
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
                userRequest = ToLower(userRequest);

                var mExternalUser = await GetExternalUserAsync(userRequest);

                mExternalUser.DisableAccount = userRequest.DisableAccount;
                var tempMExternalUser = mapper.Map<ExternalUser>(userRequest);
                mExternalUser.Claims = tempMExternalUser.Claims;

                if (!userRequest.UpdateUpPartyName.IsNullOrWhiteSpace())
                {
                    if (!await validateApiModelExternalUserLogic.ValidateApiModelAsync(ModelState, userRequest)) return BadRequest(ModelState);
                    mExternalUser.UpPartyName = userRequest.UpdateUpPartyName;
                }

                if (userRequest.UpdateLinkClaimValue != null)
                {
                    if (userRequest.UpdateLinkClaimValue.IsNullOrWhiteSpace())
                    {
                        mExternalUser.LinkClaimValue = null;
                    }
                    else
                    {
                        mExternalUser.LinkClaimValue = userRequest.UpdateLinkClaimValue;
                    }
                }

                if (userRequest.UpdateRedemptionClaimValue != null)
                {
                    if (userRequest.UpdateRedemptionClaimValue.IsNullOrWhiteSpace())
                    {
                        mExternalUser.RedemptionClaimValue = null;
                    }
                    else
                    {
                        mExternalUser.RedemptionClaimValue = userRequest.UpdateRedemptionClaimValue;
                    }
                }

                if (!userRequest.UpdateUpPartyName.IsNullOrWhiteSpace() && userRequest.UpPartyName != userRequest.UpdateUpPartyName ||                                                                 // if up-party change
                    userRequest.UpdateLinkClaimValue != null && userRequest.LinkClaimValue != userRequest.UpdateLinkClaimValue ||                                                                      // if link claim change
                    userRequest.LinkClaimValue.IsNullOrWhiteSpace() && !userRequest.UpdateLinkClaimValue.IsNullOrWhiteSpace() ||                                                                       // if link claim is added
                    userRequest.LinkClaimValue.IsNullOrWhiteSpace() && userRequest.UpdateRedemptionClaimValue != null && userRequest.RedemptionClaimValue != userRequest.UpdateRedemptionClaimValue)   // if link claim not set and redemption claim change
                {
                    var newId = await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, await GetLinkClaimHashAsync(mExternalUser.LinkClaimValue, mExternalUser.RedemptionClaimValue));
                    if (mExternalUser.Id != newId)
                    {
                        await tenantDataRepository.DeleteAsync<ExternalUser>(mExternalUser.Id);
                        mExternalUser.Id = newId;
                        await tenantDataRepository.CreateAsync(mExternalUser);
                        return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
                    }
                }

                await tenantDataRepository.UpdateAsync(mExternalUser);
                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.ExternalUserId).Name}' by up-party name '{userRequest.UpPartyName}' and link claim '{userRequest.LinkClaimValue}' or redemption claim '{userRequest.RedemptionClaimValue}'.");
                    return NotFound(typeof(Api.ExternalUserId).Name, $"{userRequest.UpPartyName}:{(!userRequest.LinkClaimValue.IsNullOrWhiteSpace() ? userRequest.LinkClaimValue : userRequest.RedemptionClaimValue)}");
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

                var mExternalUser = await GetExternalUserAsync(userRequest);
                await tenantDataRepository.DeleteAsync<ExternalUser>(mExternalUser.Id);
                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.ExternalUserId).Name}' by up-party name '{userRequest.UpPartyName}' and link claim '{userRequest.LinkClaimValue}' or redemption claim '{userRequest.RedemptionClaimValue}'.");
                    return NotFound(typeof(Api.ExternalUserId).Name, $"{userRequest.UpPartyName}:{(!userRequest.LinkClaimValue.IsNullOrWhiteSpace() ? userRequest.LinkClaimValue : userRequest.RedemptionClaimValue)}");
                }
                throw;
            }
        }

        private async Task<ExternalUser> GetExternalUserAsync(Api.ExternalUserId userRequest)
        {
            var mExternalUser = await tenantDataRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, await GetLinkClaimHashAsync(userRequest.LinkClaimValue, userRequest.RedemptionClaimValue)), required: !userRequest.LinkClaimValue.IsNullOrWhiteSpace());
            if (mExternalUser == null)
            {
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mExternalUsers, _) = await tenantDataRepository.GetListAsync<ExternalUser>(idKey, whereQuery: u => u.DataType.Equals(Constants.Models.DataType.ExternalUser) && u.UpPartyName.Equals(userRequest.UpPartyName) && u.RedemptionClaimValue.Equals(userRequest.RedemptionClaimValue));
                mExternalUser = mExternalUsers?.FirstOrDefault();
                if (mExternalUser == null)
                {
                    throw new FoxIDsDataException() { StatusCode = DataStatusCode.NotFound };
                }
            }

            return mExternalUser;
        }

        private T ToLower<T>(T userRequest) where T : Api.ExternalUserId
        {
            userRequest.UpPartyName = userRequest.UpPartyName?.ToLower();
            userRequest.RedemptionClaimValue = userRequest.RedemptionClaimValue?.ToLower();

            if (userRequest is Api.ExternalUserUpdateRequest externalUserUpdateRequest)
            {
                externalUserUpdateRequest.UpdateUpPartyName = externalUserUpdateRequest.UpdateUpPartyName?.ToLower();
                externalUserUpdateRequest.UpdateRedemptionClaimValue = externalUserUpdateRequest.UpdateRedemptionClaimValue?.ToLower();
            }

            return userRequest;
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
