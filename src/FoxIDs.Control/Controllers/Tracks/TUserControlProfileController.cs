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
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Base)]
    public class TUserControlProfileController : ApiController
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
        /// <returns>User control profile.</returns>
        [ProducesResponseType(typeof(Api.UserControlProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UserControlProfile>> GetUserControlProfile()
        {
            try
            {
                if(RouteBinding.TrackName != Constants.Routes.MasterTrackName)
                {
                    throw new Exception("User control profile only supported in master environment.");
                }

                var userHashId = await User.Identity.Name.HashIdStringAsync();

                var mUserControlProfile = await tenantRepository.GetAsync<UserControlProfile>(await UserControlProfile.IdFormatAsync(RouteBinding, userHashId));
                return Ok(mapper.Map<Api.UserControlProfile>(mUserControlProfile));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.UserControlProfile).Name}' by user sub '{User?.Identity?.Name}'.");
                    return NotFound(typeof(Api.UserControlProfile).Name, User?.Identity?.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Update user control profile.
        /// </summary>
        /// <param name="userControlProfile">User control profile.</param>
        /// <returns>User control profile.</returns>
        [ProducesResponseType(typeof(Api.UserControlProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.UserControlProfile>> PutUserControlProfile([FromBody] Api.UserControlProfile userControlProfile)
        {
            try
            {
                if (RouteBinding.TrackName != Constants.Routes.MasterTrackName)
                {
                    throw new Exception("User control profile only supported in master environment.");
                }

                var mUserControlProfile = mapper.Map<UserControlProfile>(userControlProfile);
                mUserControlProfile.Id = await UserControlProfile.IdFormatAsync(RouteBinding, await User.Identity.Name.HashIdStringAsync());
                await tenantRepository.SaveAsync(mUserControlProfile);

                return Ok(mapper.Map<Api.UserControlProfile>(mUserControlProfile));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.UserControlProfile).Name}' by user sub '{User?.Identity?.Name}'.");
                    return NotFound(typeof(Api.UserControlProfile).Name, User?.Identity?.Name);
                }
                throw;
            }
        }

        /// <summary>
        /// Delete user control profile.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUserControlProfile()
        {
            try
            {
                if (RouteBinding.TrackName != Constants.Routes.MasterTrackName)
                {
                    throw new Exception("User control profile only supported in master environment.");
                }

                var userHashId = await User.Identity.Name.HashIdStringAsync();

                _ = await tenantRepository.DeleteAsync<UserControlProfile>(await UserControlProfile.IdFormatAsync(RouteBinding, userHashId));
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.UserControlProfile).Name}' by user sub '{User?.Identity?.Name}'.");
                    return NotFound(typeof(Api.UserControlProfile).Name, User?.Identity?.Name);
                }
                throw;
            }
        }
    }
}
