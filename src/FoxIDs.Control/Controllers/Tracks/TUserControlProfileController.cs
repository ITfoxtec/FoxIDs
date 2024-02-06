using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using ITfoxtec.Identity;
using System;

namespace FoxIDs.Controllers
{
    public class TUserControlProfileController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TUserControlProfileController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Get user control profile.
        /// </summary>
        /// <param name="userSub">User sub.</param>
        /// <returns>User control profile.</returns>
        [ProducesResponseType(typeof(Api.UserControlProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UserControlProfile>> GetUserControlProfile(string userSub)
        {
            try
            {
                if(RouteBinding.TrackName != Constants.Routes.MasterTrackName)
                {
                    throw new Exception("User control profile only supported in master track.");
                }

                if (!ModelState.TryValidateRequiredParameter(userSub, nameof(userSub))) return BadRequest(ModelState);

                var userHashId = await userSub.ToLower().Sha256HashBase64urlEncodedAsync();

                var mUserControlProfile = await tenantRepository.GetAsync<UserControlProfile>(await UserControlProfile.IdFormatAsync(RouteBinding, userHashId));
                return Ok(mapper.Map<Api.UserControlProfile>(mUserControlProfile));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.UserControlProfile).Name}' by email '{userSub}'.");
                    return NotFound(typeof(Api.UserControlProfile).Name, userSub);
                }
                throw;
            }
        }

        /// <summary>
        /// Update user control profile.
        /// </summary>
        /// <param name="userControlProfileRequest">User control profile.</param>
        /// <returns>User control profile.</returns>
        [ProducesResponseType(typeof(Api.UserControlProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UserControlProfile>> PutUserControlProfile([FromBody] Api.UserControlProfileRequest userControlProfileRequest)
        {
            try
            {
                if (RouteBinding.TrackName != Constants.Routes.MasterTrackName)
                {
                    throw new Exception("User control profile only supported in master track.");
                }

                if (!await ModelState.TryValidateObjectAsync(userControlProfileRequest)) return BadRequest(ModelState);
                userControlProfileRequest.UserSub = userControlProfileRequest.UserSub?.ToLower();

                var mUserControlProfile = mapper.Map<UserControlProfile>(userControlProfileRequest);
                await tenantRepository.SaveAsync(mUserControlProfile);

                return Ok(mapper.Map<Api.UserControlProfile>(mUserControlProfile));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.UserControlProfile).Name}' by userSub '{userControlProfileRequest.UserSub}'.");
                    return NotFound(typeof(Api.UserControlProfile).Name, userControlProfileRequest.UserSub, userControlProfileRequest.UserSub);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete user control profile.
        /// </summary>
        /// <param name="email">User control profile email.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUserControlProfile(string email)
        {
            try
            {
                if (RouteBinding.TrackName != Constants.Routes.MasterTrackName)
                {
                    throw new Exception("User control profile only supported in master track.");
                }

                if (!ModelState.TryValidateRequiredParameter(email, nameof(email))) return BadRequest(ModelState);
                email = email?.ToLower();

                await tenantRepository.DeleteAsync<UserControlProfile>(await Models.UserControlProfile.IdFormatAsync(RouteBinding, email));
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.UserControlProfile).Name}' by email '{email}'.");
                    return NotFound(typeof(Api.UserControlProfile).Name, email);
                }
                throw;
            }
        }
    }
}
