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
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TExternalUserController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TExternalUserController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Get external user.
        /// </summary>
        /// <returns>External user.</returns>
        [ProducesResponseType(typeof(Api.ExternalUser), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ExternalUser>> GetExternalUser(Api.ExternalUserIdRequest userRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(userRequest)) return BadRequest(ModelState);

                var linkClaimHash = userRequest.LinkClaimHash ?? await userRequest.LinkClaim.ToLower().Sha256HashBase64urlEncodedAsync();

                var mExternalUser = await tenantRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, linkClaimHash));
                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.ExternalUser).Name}' by up-party name '{userRequest.UpPartyName}' and link claim hash '{userRequest.LinkClaimHash}' or link claim '{userRequest.LinkClaim}'.");
                    return NotFound(typeof(Api.ExternalUser).Name, $"{userRequest.UpPartyName}:{userRequest.LinkClaimHash ?? userRequest.LinkClaim}");
                }
                throw;
            }
        }

        /// <summary>
        /// Create external user.
        /// </summary>
        /// <param name="createExternalUserRequest">ExternalUser.</param>
        /// <returns>ExternalUser.</returns>
        [ProducesResponseType(typeof(Api.ExternalUser), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.ExternalUser>> PostExternalUser([FromBody] Api.CreateExternalUserRequest createExternalUserRequest)
        {

            try
            {
                if (!await ModelState.TryValidateObjectAsync(createExternalUserRequest)) return BadRequest(ModelState);
                var linkClaimHash = await createExternalUserRequest.LinkClaim.ToLower().Sha256HashBase64urlEncodedAsync();

                var mExternalUser = mapper.Map<ExternalUser>(createExternalUserRequest);
                mExternalUser.Id = await ExternalUser.IdFormatAsync(RouteBinding, createExternalUserRequest.UpPartyName, linkClaimHash);
                await tenantRepository.CreateAsync(mExternalUser);

                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.ExternalUserIdRequest).Name}' by up-party name '{createExternalUserRequest.UpPartyName}' and link claim '{createExternalUserRequest.LinkClaim}'.");
                    return Conflict(typeof(Api.ExternalUserIdRequest).Name, $"{createExternalUserRequest.UpPartyName}:{createExternalUserRequest.LinkClaim}");
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

                var linkClaimHash = externalUserRequest.LinkClaimHash ?? await externalUserRequest.LinkClaim.ToLower().Sha256HashBase64urlEncodedAsync();
               
                var mExternalUser = await tenantRepository.GetAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, externalUserRequest.UpPartyName, linkClaimHash));

                mExternalUser.Claims = mapper.Map<ExternalUser>(externalUserRequest).Claims;   
                await tenantRepository.SaveAsync(mExternalUser);

                return Ok(mapper.Map<Api.ExternalUser>(mExternalUser));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.ExternalUserIdRequest).Name}' by up-party name '{externalUserRequest.UpPartyName}' and link claim hash '{externalUserRequest.LinkClaimHash}' or link claim '{externalUserRequest.LinkClaim}'.");
                    return NotFound(typeof(Api.ExternalUserIdRequest).Name, $"{externalUserRequest.UpPartyName}:{externalUserRequest.LinkClaimHash ?? externalUserRequest.LinkClaim}");
                }
                throw;
            }
        }

        /// <summary>
        /// Delete external user.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteExternalUser(Api.ExternalUserIdRequest userRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(userRequest)) return BadRequest(ModelState);

                var linkClaimHash = userRequest.LinkClaimHash ?? await userRequest.LinkClaim.ToLower().Sha256HashBase64urlEncodedAsync();


                _ = await tenantRepository.DeleteAsync<ExternalUser>(await ExternalUser.IdFormatAsync(RouteBinding, userRequest.UpPartyName, linkClaimHash));
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.CreateExternalUserRequest).Name}' by up-party name '{userRequest.UpPartyName}' and link claim hash '{userRequest.LinkClaimHash}' or link claim '{userRequest.LinkClaim}'.");
                    return NotFound(typeof(Api.CreateExternalUserRequest).Name, $"{userRequest.UpPartyName}:{userRequest.LinkClaimHash ?? userRequest.LinkClaim}");
                }
                throw;
            }
        }
    }
}
