using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TUserSetPasswordController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly BaseAccountLogic accountLogic;

        public TUserSetPasswordController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, BaseAccountLogic accountLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.accountLogic = accountLogic;
        }

        /// <summary>
        /// Set the users password.
        /// Validate the password policy including password history update if a password is set. Not validated if the password is set with a password hash.
        /// You can set a plaintext password, provide password hash fields, or leave the password fields empty to remove the current password; never include both password formats in the same request.
        /// </summary>
        /// <param name="request">User password.</param>
        /// <returns>User.</returns>
        [ProducesResponseType(typeof(Api.User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.User>> PutUserSetPassword([FromBody] Api.UserSetPasswordRequest request)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(request)) return BadRequest(ModelState);
                request.Email = request.Email?.Trim().ToLower();
                request.Phone = request.Phone?.Trim();
                request.Username = request.Username?.Trim()?.ToLower();

                var mUser = await tenantDataRepository.GetAsync<User>(await Models.User.IdFormatAsync(RouteBinding, new User.IdKey { Email = request.Email, UserIdentifier = request.Phone ?? request.Username }), queryAdditionalIds: true);
                await accountLogic.SetPasswordUserAsync(mUser, new SetPasswordObj
                {
                    Password = request.Password,
                    PasswordHashAlgorithm = request.PasswordHashAlgorithm,
                    PasswordHash = request.PasswordHash,
                    PasswordHashSalt = request.PasswordHashSalt,
                    PasswordLastChanged = request.PasswordLastChanged,
                    ChangePassword = request.ChangePassword
                });

                return Ok(mapper.Map<Api.User>(mUser));
            }
            catch (AccountException aex)
            {
                ModelState.TryAddModelError(nameof(request.Password), aex.Message);
                return BadRequest(ModelState, aex);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Set password on '{typeof(Api.User).Name}' by email '{request.Email}', phone '{request.Phone}', username '{request.Username}'.");
                    return NotFound(typeof(Api.User).Name, new { request.Email, request.Phone, request.Username }.ToJson());
                }
                throw;
            }
        }
    }
}
