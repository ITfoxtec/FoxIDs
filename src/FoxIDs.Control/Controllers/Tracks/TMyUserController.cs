using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using System;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Basic)]
    public class TMyUserController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TMyUserController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get my user.
        /// </summary>
        /// <returns>User.</returns>
        [ProducesResponseType(typeof(Api.MyUser), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.MyUser>> GetMyUser()
        {
            try
            {
                var mUser = await tenantDataRepository.GetAsync<User>(await GetMyIdAsync());
                return Ok(mapper.Map<Api.MyUser>(mUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.MyUser).Name}' by claims.");
                    return NotFound(typeof(Api.MyUser).Name, "by claims");
                }
                throw;
            }
        }

        /// <summary>
        /// Update my user.
        ///  - It is only possible to set the ChangePassword property.
        /// </summary>
        /// <param name="user">My user.</param>
        /// <returns>My user.</returns>
        [ProducesResponseType(typeof(Api.MyUser), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.MyUser>> PutMyUser([FromBody] Api.MyUser user)
        {
            try
            {
                var mUser = await tenantDataRepository.GetAsync<User>(await GetMyIdAsync());
                mUser.ChangePassword = user.ChangePassword;
                await tenantDataRepository.UpdateAsync(mUser);

                return Ok(mapper.Map<Api.MyUser>(mUser));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.MyUser).Name}' by claims.");
                    return NotFound(typeof(Api.MyUser).Name, "by claims");
                }
                throw;
            }
        }

        private async Task<string> GetMyIdAsync()
        {
            var email = User.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Email);
            var phone = User.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.PhoneNumber);
            var username = User.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.PreferredUsername);
            var userId = User.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject);

            if (email.IsNullOrWhiteSpace() && phone.IsNullOrWhiteSpace() && username.IsNullOrWhiteSpace() && userId.IsNullOrWhiteSpace())
            {
                throw new Exception("Authenticated users email, phone, username and userid claims is empty, at lease one is required.");
            }

            return await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { Email = email, UserIdentifier = phone ?? username, UserId = userId });
        }
    }
}
