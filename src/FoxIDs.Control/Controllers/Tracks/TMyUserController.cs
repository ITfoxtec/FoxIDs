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
    public class TMyUserController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TMyUserController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Get my user.
        /// </summary>
        /// <returns>User.</returns>
        [ProducesResponseType(typeof(Api.MyUser), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.MyUser>> GetMyUser()
        {
            var email = User.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Email);
            try
            {
                if (email.IsNullOrWhiteSpace())
                {
                    throw new Exception("Authenticated users email claim is empty.");
                }
                var mUser = await tenantRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, email));
                return Ok(mapper.Map<Api.MyUser>(mUser));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.MyUser).Name}' by email '{email}'.");
                    return NotFound(typeof(Api.MyUser).Name, email);
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
            var email = User.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Email);
            try
            {
                if (email.IsNullOrWhiteSpace())
                {
                    throw new Exception("Authenticated users email claim is empty.");
                }
                var mUser = await tenantRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, email));
                mUser.ChangePassword = user.ChangePassword;
                await tenantRepository.UpdateAsync(mUser);

                return Ok(mapper.Map<Api.MyUser>(mUser));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.MyUser).Name}' by email '{email}'.");
                    return NotFound(typeof(Api.MyUser).Name, email, nameof(email));
                }
                throw;
            }
        }
    }
}
