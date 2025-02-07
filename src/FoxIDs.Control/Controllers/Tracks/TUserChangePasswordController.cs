using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Logic;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TUserChangePasswordController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly BaseAccountLogic accountLogic;

        public TUserChangePasswordController(TelemetryScopedLogger logger, IMapper mapper, BaseAccountLogic accountLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.accountLogic = accountLogic;
        }

        /// <summary>
        /// Change the users password.
        /// </summary>
        /// <param name="userRequest">User with current and new password.</param>
        /// <returns>User.</returns>
        [ProducesResponseType(typeof(Api.User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.User>> PutUserChangePassword([FromBody] Api.UserChangePasswordRequest userRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(userRequest)) return BadRequest(ModelState);
                userRequest.Email = userRequest.Email?.Trim().ToLower();
                userRequest.Phone = userRequest.Phone?.Trim();
                userRequest.Username = userRequest.Username?.Trim()?.ToLower();

                var mUser = await accountLogic.ChangePasswordUserAsync(new UserIdentifier { Email = userRequest.Email, Phone = userRequest.Phone, Username = userRequest.Username }, userRequest.CurrentPassword, userRequest.NewPassword);

                return Ok(mapper.Map<Api.User>(mUser));
            }
            catch (UserNotExistsException ueex)
            {
                logger.Warning(ueex, $"NotFound, Change password on '{typeof(Api.User).Name}' by email '{userRequest.Email}'.");
                return NotFound(ueex.Message);
            }
            catch (InvalidPasswordException ipex)
            {
                logger.ScopeTrace(() => ipex.Message, triggerEvent: true);
                ModelState.TryAddModelError(userRequest.CurrentPassword, "Wrong password");
                return BadRequest(ModelState, ipex);
            }
            catch (NewPasswordEqualsCurrentException npeex)
            {
                logger.ScopeTrace(() => npeex.Message);
                ModelState.AddModelError(nameof(userRequest.NewPassword), "Please use a new password.");
                return BadRequest(ModelState, npeex);
            }
            catch (PasswordLengthException plex)
            {
                logger.ScopeTrace(() => plex.Message);
                ModelState.AddModelError(nameof(userRequest.NewPassword), RouteBinding.CheckPasswordComplexity ?
                    $"Please use {RouteBinding.PasswordLength} characters or more with a mix of letters, numbers and symbols." :
                    $"Please use {RouteBinding.PasswordLength} characters or more.");
                return BadRequest(ModelState, plex);
            }
            catch (PasswordComplexityException pcex)
            {
                logger.ScopeTrace(() => pcex.Message);
                ModelState.AddModelError(nameof(userRequest.NewPassword), "Please use a mix of letters, numbers and symbols");
                return BadRequest(ModelState, pcex);
            }
            catch (PasswordEmailTextComplexityException pecex)
            {
                logger.ScopeTrace(() => pecex.Message);
                ModelState.AddModelError(nameof(userRequest.NewPassword), "Please do not use the email or parts of it.");
                return BadRequest(ModelState, pecex);
            }
            catch (PasswordPhoneTextComplexityException ppcex)
            {
                logger.ScopeTrace(() => ppcex.Message);
                ModelState.AddModelError(nameof(userRequest.NewPassword), "Please do not use the phone number.");
                return BadRequest(ModelState, ppcex);
            }
            catch (PasswordUsernameTextComplexityException pucex)
            {
                logger.ScopeTrace(() => pucex.Message);
                ModelState.AddModelError(nameof(userRequest.NewPassword), "Please do not use the username or parts of it.");
                return BadRequest(ModelState, pucex);
            }
            catch (PasswordUrlTextComplexityException pucex)
            {
                logger.ScopeTrace(() => pucex.Message);
                ModelState.AddModelError(nameof(userRequest.NewPassword), "Please do not use parts of the URL.");
                return BadRequest(ModelState, pucex);
            }
            catch (PasswordRiskException prex)
            {
                logger.ScopeTrace(() => prex.Message);
                ModelState.AddModelError(nameof(userRequest.NewPassword), "The password has previously appeared in a data breach. Please choose a more secure alternative.");
                return BadRequest(ModelState, prex);

            }
        }
    }
}
