using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// External login secret for authentication method API.
    /// </summary>
    public class TExternalLoginSecretUpPartyController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;

        public TExternalLoginSecretUpPartyController(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get External login secret for authentication method.
        /// </summary>
        /// <param name="partyName">External login authentication method name.</param>
        /// <returns>External login secret for authentication method.</returns>
        [ProducesResponseType(typeof(List<Api.ExternalLoginSecretResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.ExternalLoginSecretResponse>> GetExternalLoginSecretUpParty(string partyName)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(partyName, nameof(partyName))) return BadRequest(ModelState);
                partyName = partyName?.ToLower();

                var extLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));
                if (!string.IsNullOrWhiteSpace(extLoginUpParty?.Secret))
                {
                    return Ok(new Api.ExternalLoginSecretResponse
                    {
                        Info = extLoginUpParty.Secret.GetShortSecret(false),
                    });
                }
                else
                {
                    return Ok(new Api.ExternalLoginSecretResponse());
                }
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(ExternalLoginUpParty).Name}' client key by name '{partyName}'.");
                    return NotFound(typeof(ExternalLoginUpParty).Name, partyName);
                }
                throw;
            }
        }

        /// <summary>
        /// Update External login secret for authentication method.
        /// </summary>
        /// <param name="secretRequest">External login secret for authentication method.</param>
        [ProducesResponseType(typeof(Api.OAuthUpParty), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Api.OAuthUpParty>> PutExternalLoginSecretUpParty([FromBody] Api.ExternalLoginSecretRequest secretRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(secretRequest)) return BadRequest(ModelState);
                secretRequest.PartyName = secretRequest.PartyName?.ToLower();

                var extLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(await UpParty.IdFormatAsync(RouteBinding, secretRequest.PartyName));
                if (extLoginUpParty.ExternalLoginType == ExternalConnectTypes.Api && secretRequest.Secret.IsNullOrEmpty())
                {
                    throw new Exception($"Client secret is require if '{nameof(extLoginUpParty.ExternalLoginType)}' is '{ExternalConnectTypes.Api}'");
                }

                extLoginUpParty.Secret = secretRequest.Secret;
                await tenantDataRepository.UpdateAsync(extLoginUpParty);

                return Created(new Api.ExternalLoginUpParty { Name = secretRequest.PartyName });
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create client key on client '{typeof(ExternalLoginUpParty).Name}' by name '{secretRequest.PartyName}'.");
                    return Conflict(typeof(ExternalLoginUpParty).Name, secretRequest.PartyName, nameof(secretRequest.PartyName));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete External login secret for authentication method.
        /// </summary>
        /// <param name="name">Authentication method name.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteExternalLoginSecretUpParty(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);

                var partyName = name?.ToLower();
                var extLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));
                if (extLoginUpParty.ExternalLoginType == ExternalConnectTypes.Api)
                {
                    throw new Exception($"Client secret is require if '{nameof(extLoginUpParty.ExternalLoginType)}' is '{ExternalConnectTypes.Api}'");
                }

                extLoginUpParty.Secret = null;
                await tenantDataRepository.UpdateAsync(extLoginUpParty);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete client secret from client '{typeof(ExternalLoginUpParty).Name}' by name '{name}'.");
                    return NotFound(typeof(ExternalLoginUpParty).Name, name);
                }
                throw;
            }
        }
    }
}
