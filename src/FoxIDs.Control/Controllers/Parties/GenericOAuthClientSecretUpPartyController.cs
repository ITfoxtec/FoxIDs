using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using System;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Abstract OAuth 2.0 import client secret for authentication method API.
    /// </summary>
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public abstract class GenericOAuthClientSecretUpPartyController<TParty, TClient> : ApiController where TParty : OAuthUpParty<TClient> where TClient : OAuthUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;

        public GenericOAuthClientSecretUpPartyController(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
        }

        protected async Task<ActionResult<Api.OAuthClientSecretSingleResponse>> Get(string partyName)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(partyName, nameof(partyName))) return BadRequest(ModelState);
                partyName = partyName?.ToLower();

                var oauthUpParty = await tenantDataRepository.GetAsync<TParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));
                if (!string.IsNullOrWhiteSpace(oauthUpParty?.Client?.ClientSecret))
                {
                    return Ok(new Api.OAuthClientSecretSingleResponse
                    {
                        Info = oauthUpParty.Client.ClientSecret.Length > 20 ? oauthUpParty.Client.ClientSecret.Substring(0, 3) : oauthUpParty.Client.ClientSecret,
                    });
                }
                else
                {
                    return Ok(new Api.OAuthClientSecretSingleResponse());
                }
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(TParty).Name}' client key by name '{partyName}'.");
                    return NotFound(typeof(TParty).Name, partyName);
                }
                throw;
            }
        }

        protected async Task<ActionResult<Api.OAuthUpParty>> Put([FromBody] Api.OAuthClientSecretSingleRequest secretRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(secretRequest)) return BadRequest(ModelState);
                secretRequest.PartyName = secretRequest.PartyName?.ToLower();

                var oauthUpParty = await tenantDataRepository.GetAsync<TParty>(await UpParty.IdFormatAsync(RouteBinding, secretRequest.PartyName));
                if (oauthUpParty.Client.ClientAuthenticationMethod != ClientAuthenticationMethods.PrivateKeyJwt && secretRequest.Secret.IsNullOrEmpty())
                {
                    throw new Exception($"Client secret is require if 'ClientAuthenticationMethod' is different from '{ClientAuthenticationMethods.PrivateKeyJwt}'");
                }

                oauthUpParty.Client.ClientSecret = secretRequest.Secret;
                await tenantDataRepository.UpdateAsync(oauthUpParty);

                return Created(new Api.OAuthUpParty { Name = secretRequest.PartyName });
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create client key on client '{typeof(TParty).Name}' by name '{secretRequest.PartyName}'.");
                    return Conflict(typeof(TParty).Name, secretRequest.PartyName, nameof(secretRequest.PartyName));
                }
                throw;
            }
        }

        protected async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);

                var partyName = name?.ToLower();
                var oauthUpParty = await tenantDataRepository.GetAsync<TParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));
                if (oauthUpParty.Client.ClientAuthenticationMethod != ClientAuthenticationMethods.PrivateKeyJwt)
                {
                    throw new Exception($"Client secret is require if 'ClientAuthenticationMethod' is different from '{ClientAuthenticationMethods.PrivateKeyJwt}'");
                }

                oauthUpParty.Client.ClientSecret = null;
                await tenantDataRepository.UpdateAsync(oauthUpParty);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete client secret from client '{typeof(TParty).Name}' by name '{name}'.");
                    return NotFound(typeof(TParty).Name, name);
                }
                throw;
            }
        }
    }
}
