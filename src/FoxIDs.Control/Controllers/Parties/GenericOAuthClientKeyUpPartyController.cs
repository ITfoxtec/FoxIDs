﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
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
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Abstract OAuth 2.0 import client key for authentication method API.
    /// </summary>
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public abstract class GenericOAuthClientKeyUpPartyController<TParty, TClient> : ApiController where TParty : OAuthUpParty<TClient> where TClient : OAuthUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PlanCacheLogic planCacheLogic;

        public GenericOAuthClientKeyUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PlanCacheLogic planCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.planCacheLogic = planCacheLogic;
        }

        protected async Task<ActionResult<Api.OAuthClientKeyResponse>> Get(string partyName)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(partyName, nameof(partyName))) return BadRequest(ModelState);
                partyName = partyName?.ToLower();

                var oauthUpParty = await tenantDataRepository.GetAsync<TParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));
                if (oauthUpParty.Client.ClientKeys?.Count() > 0)
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

        protected async Task<ActionResult<Api.OAuthClientKeyResponse>> Post([FromBody] Api.OAuthClientKeyRequest keyRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(keyRequest)) return BadRequest(ModelState);
                keyRequest.PartyName = keyRequest.PartyName?.ToLower();

                var oauthUpParty = await tenantDataRepository.GetAsync<TParty>(await UpParty.IdFormatAsync(RouteBinding, keyRequest.PartyName));

                var certificate = keyRequest.Password.IsNullOrWhiteSpace() switch
                {
                    //Can not be change to X509CertificateLoader LoadPkcs12 or LoadCertificate because it should automatically select between the two methods.
                    true => new X509Certificate2(WebEncoders.Base64UrlDecode(keyRequest.Certificate), string.Empty, keyStorageFlags: X509KeyStorageFlags.Exportable),
                    false => new X509Certificate2(WebEncoders.Base64UrlDecode(keyRequest.Certificate), keyRequest.Password, keyStorageFlags: X509KeyStorageFlags.Exportable),
                };
                if (!keyRequest.Password.IsNullOrWhiteSpace() && !certificate.HasPrivateKey)
                {
                    throw new ValidationException("Unable to read the certificates private key. E.g, try to convert the certificate and save the certificate with 'TripleDES-SHA1'.");
                }

                var jwt = await certificate.ToFTJsonWebKeyAsync(includePrivateKey: true);
                var clientKey = new ClientKey
                {
                    Type = ClientKeyTypes.Contained,
                    ExternalName = Guid.NewGuid().ToString(),
                    Key = jwt,
                    PublicKey = jwt.GetPublicKey()
                };

                var secondaryKey = oauthUpParty.Client.ClientKeys?.Count() > 1 ? oauthUpParty.Client.ClientKeys[2] : null;
                oauthUpParty.Client.ClientKeys = new List<ClientKey>
                {
                    clientKey
                };
                if (secondaryKey != null)
                {
                    oauthUpParty.Client.ClientKeys.Add(secondaryKey);
                }

                if (!await ModelState.TryValidateObjectAsync(keyRequest)) return BadRequest(ModelState);
                await tenantDataRepository.UpdateAsync(oauthUpParty);

                return Created(new Api.OAuthClientKeyResponse
                {
                    Name = clientKey.ExternalName,
                    PrimaryKey = mapper.Map<Api.ClientKey>(clientKey)
                });
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
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

                var oauthUpParty = await tenantDataRepository.GetAsync<TParty>(await UpParty.IdFormatAsync(RouteBinding, partyName));

                var key = oauthUpParty.Client.ClientKeys?.Where(k => k.ExternalName == externalName).FirstOrDefault();
                if (key != null)
                {
                    oauthUpParty.Client.ClientKeys.Remove(key);
                    await tenantDataRepository.UpdateAsync(oauthUpParty);
                }

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete client key from client '{typeof(TParty).Name}' by name '{name}'.");
                    return NotFound(typeof(TParty).Name, name);
                }
                throw;
            }
        }
    }
}
