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

namespace FoxIDs.Controllers
{
    /// <summary>
    /// OIDC import client key for up-party API.
    /// </summary>
    public class TOidcImportClientKeyUpPartyController : TenantApiController 
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly ExternalKeyLogic externalKeyLogic;

        public TOidcImportClientKeyUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, ExternalKeyLogic externalKeyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.externalKeyLogic = externalKeyLogic;
        }

        protected async Task<ActionResult<Api.OidcClientKeyResponse>> Get(string partyName)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(partyName, nameof(partyName))) return BadRequest(ModelState);
                partyName = partyName?.ToLower();

                var oidcUpParty = await tenantRepository.GetAsync<OidcUpParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));
                if (oidcUpParty.Client.ClientKeys?.Count() > 0 && oidcUpParty.Client.ClientKeys.First().Type == ClientKeyTypes.KeyVaultUpload)
                {
                    return Ok(new Api.OidcClientKeyResponse { PublicCertificate = WebEncoders.Base64UrlEncode(oidcUpParty.Client.ClientKeys.First().PublicKey.ToX509Certificate().RawData) });
                }
                else
                {
                    return Ok(new Api.OidcClientKeyResponse());
                }
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(OidcUpParty).Name}' client key by name '{partyName}'.");
                    return NotFound(typeof(OidcUpParty).Name, partyName);
                }
                throw;
            }
        }

        protected async Task<ActionResult<Api.OidcClientKeyResponse>> Post(Api.OidcClientKeyRequest keyRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(keyRequest)) return BadRequest(ModelState);
                keyRequest.PartyName = keyRequest.PartyName?.ToLower();

                var oidcUpParty = await tenantRepository.GetAsync<OidcUpParty>(await UpParty.IdFormatAsync(RouteBinding, keyRequest.PartyName));

                (var externalName, var publicCertificate, var externalId) = await externalKeyLogic.ImportExternalKeyAsync(WebEncoders.Base64UrlDecode(keyRequest.Certificate), keyRequest.Password, upPartyName: keyRequest.PartyName);
                var publicKey = new X509Certificate2(publicCertificate).ToFTJsonWebKey();

                var secondaryKey = oidcUpParty.Client.ClientKeys?.Count() > 1 ? oidcUpParty.Client.ClientKeys[2] : null;
                oidcUpParty.Client.ClientKeys = new List<ClientKey>
                {
                    new ClientKey
                    {
                        Type = ClientKeyTypes.KeyVaultUpload,
                        ExternalName = externalName,
                        PublicKey = publicKey,
                        ExternalId = externalId,
                    }
                };
                if (secondaryKey != null)
                {
                    oidcUpParty.Client.ClientKeys.Add(secondaryKey);
                }

                if (!await ModelState.TryValidateObjectAsync(keyRequest)) return BadRequest(ModelState);
                await tenantRepository.UpdateAsync(oidcUpParty);

                return Created(new Api.OidcUpParty { Name = keyRequest.PartyName });
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create client key on client '{typeof(OidcUpParty).Name}' by name '{keyRequest.PartyName}'.");
                    return Conflict(typeof(OidcUpParty).Name, keyRequest.PartyName, nameof(keyRequest.PartyName));
                }
                throw;
            }
        }

        protected async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);
                name = name?.ToLower();

                var partyName = name.GetFirstInDotList();
                var externalName = name.GetLastInDotList();
                var oidcUpParty = await tenantRepository.GetAsync<OidcUpParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));

                var key = oidcUpParty.Client.ClientKeys?.Select(k => k.Type == ClientKeyTypes.KeyVaultUpload && k.ExternalName == externalName).FirstOrDefault();
                if (key != null)
                {
                    await externalKeyLogic.DeleteExternalKeyAsync(externalName);
                    oidcUpParty.Client.ClientKeys = oidcUpParty.Client.ClientKeys.Where(k => k.ExternalName != externalName).ToList();
                }

                await tenantRepository.UpdateAsync(oidcUpParty);

                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete client key from client '{typeof(OidcUpParty).Name}' by name '{name}'.");
                    return NotFound(typeof(OidcUpParty).Name, name);
                }
                throw;
            }
        }
    }
}
