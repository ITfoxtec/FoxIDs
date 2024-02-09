using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using System.Collections.Generic;
using FoxIDs.Logic;
using Microsoft.AspNetCore.WebUtilities;
using ITfoxtec.Identity;
using System.Security.Cryptography.X509Certificates;
using System;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Abstract OAuth 2.0 import client key for up-party API.
    /// </summary>
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public abstract class GenericOAuthClientKeyUpPartyController<TParty, TClient> : ApiController where TParty : OAuthUpParty<TClient> where TClient : OAuthUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly PlanCacheLogic planCacheLogic;
        private readonly ExternalKeyLogic externalKeyLogic;

        public GenericOAuthClientKeyUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, PlanCacheLogic planCacheLogic, ExternalKeyLogic externalKeyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.planCacheLogic = planCacheLogic;
            this.externalKeyLogic = externalKeyLogic;
        }

        protected async Task<ActionResult<Api.OAuthClientKeyResponse>> Get(string partyName)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(partyName, nameof(partyName))) return BadRequest(ModelState);
                partyName = partyName?.ToLower();

                var oauthUpParty = await tenantRepository.GetAsync<TParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));
                if (oauthUpParty.Client.ClientKeys?.Count() > 0 && oauthUpParty.Client.ClientKeys.First().Type == ClientKeyTypes.KeyVaultImport)
                {
                    var clientKey = oauthUpParty.Client.ClientKeys.First();
                    return Ok(new Api.OAuthClientKeyResponse
                    {
                        Name = clientKey.ExternalName,
                        PrimaryKey = mapper.Map<Api.ClientKey>(clientKey)
                    });
                }
                else
                {
                    return Ok(new Api.OAuthClientKeyResponse());
                }
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(TParty).Name}' client key by name '{partyName}'.");
                    return NotFound(typeof(TParty).Name, partyName);
                }
                throw;
            }
        }

        protected async Task<ActionResult<Api.OAuthClientKeyResponse>> Post([FromBody] Api.OAuthClientKeyRequest keyRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(keyRequest)) return BadRequest(ModelState);
                keyRequest.PartyName = keyRequest.PartyName?.ToLower();

                var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                if (!plan.EnableKeyVault)
                {
                    throw new Exception($"Key Vault and thereby client certificates is not supported in the '{plan.Name}' plan.");
                }

                var oauthUpParty = await tenantRepository.GetAsync<TParty>(await UpParty.IdFormatAsync(RouteBinding, keyRequest.PartyName));

                (var externalName, var publicCertificate, var externalId) = await externalKeyLogic.ImportExternalKeyAsync(WebEncoders.Base64UrlDecode(keyRequest.Certificate), keyRequest.Password, upPartyName: keyRequest.PartyName);
                var publicKey = new X509Certificate2(publicCertificate).ToFTJsonWebKey();

                var secondaryKey = oauthUpParty.Client.ClientKeys?.Count() > 1 ? oauthUpParty.Client.ClientKeys[2] : null;
                oauthUpParty.Client.ClientKeys = new List<ClientKey>
                {
                    new ClientKey
                    {
                        Type = ClientKeyTypes.KeyVaultImport,
                        ExternalName = externalName,
                        PublicKey = publicKey,
                        ExternalId = externalId,
                    }
                };
                if (secondaryKey != null)
                {
                    oauthUpParty.Client.ClientKeys.Add(secondaryKey);
                }

                if (!await ModelState.TryValidateObjectAsync(keyRequest)) return BadRequest(ModelState);
                await tenantRepository.UpdateAsync(oauthUpParty);

                var clientKey = oauthUpParty.Client.ClientKeys.First();
                return Created(new Api.OAuthClientKeyResponse
                {
                    Name = clientKey.ExternalName,
                    PrimaryKey = mapper.Map<Api.ClientKey>(clientKey)
                });
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create client key on client '{typeof(TParty).Name}' by name '{keyRequest.PartyName}'.");
                    return Conflict(typeof(TParty).Name, keyRequest.PartyName, nameof(keyRequest.PartyName));
                }
                throw;
            }
        }

        protected async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);

                var partyName = name?.ToLower().GetFirstInDotList();
                if (!ModelState.TryValidateRequiredParameter(partyName, $"{nameof(name)}[0]")) return BadRequest(ModelState);
                var externalName = name.GetLastInDotList();
                if (!ModelState.TryValidateRequiredParameter(externalName, $"{nameof(name)}[1]")) return BadRequest(ModelState);

                var oauthUpParty = await tenantRepository.GetAsync<TParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));

                var key = oauthUpParty.Client.ClientKeys?.Where(k => k.Type == ClientKeyTypes.KeyVaultImport && k.ExternalName == externalName).FirstOrDefault();
                if (key != null)
                {
                    oauthUpParty.Client.ClientKeys.Remove(key);
                    await tenantRepository.UpdateAsync(oauthUpParty);
                    await externalKeyLogic.DeleteExternalKeyAsync(externalName);
                }

                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete client key from client '{typeof(TParty).Name}' by name '{name}'.");
                    return NotFound(typeof(TParty).Name, name);
                }
                throw;
            }
        }
    }
}
