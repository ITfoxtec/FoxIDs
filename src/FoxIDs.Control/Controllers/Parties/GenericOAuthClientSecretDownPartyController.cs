﻿using FoxIDs.Infrastructure;
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
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Abstract OAuth 2.0 client secret for down-party API.
    /// </summary>
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public abstract class GenericOAuthClientSecretDownPartyController<TParty, TClient, TScope, TClaim> : ApiController where TParty : OAuthDownParty<TClient, TScope, TClaim> where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;
        private readonly SecretHashLogic secretHashLogic;

        public GenericOAuthClientSecretDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository, SecretHashLogic secretHashLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
            this.secretHashLogic = secretHashLogic;
        }

        protected async Task<ActionResult<List<Api.OAuthClientSecretResponse>>> Get(string partyName)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(partyName, nameof(partyName))) return BadRequest(ModelState);
                partyName = partyName?.ToLower();

                var oauthDownParty = await tenantRepository.GetAsync<TParty>(await DownParty.IdFormatAsync(RouteBinding, partyName));
                if (oauthDownParty?.Client?.Secrets?.Count > 0)
                {
                    return Ok(mapper.Map<List<Api.OAuthClientSecretResponse>>(oauthDownParty.Client.Secrets).Set(s => s.ForEach(es => es.Name = new[] { partyName, es.Name }.ToDotList())));
                }
                else
                {
                    return Ok(new List<Api.OAuthClientSecretResponse>());
                }
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(TParty).Name}' client secrets by name '{partyName}'.");
                    return NotFound(typeof(TParty).Name, partyName);
                }
                throw;
            }
        }

        protected async Task<ActionResult> Post(Api.OAuthClientSecretRequest secretRequest)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(secretRequest)) return BadRequest(ModelState);
                secretRequest.PartyName = secretRequest.PartyName?.ToLower();

                var oauthDownParty = await tenantRepository.GetAsync<TParty>(await DownParty.IdFormatAsync(RouteBinding, secretRequest.PartyName));

                foreach(var s in secretRequest.Secrets)
                {
                    var secret = new OAuthClientSecret();
                    await secretHashLogic.AddSecretHashAsync(secret, s);
                    if (oauthDownParty.Client.Secrets == null)
                    {
                        oauthDownParty.Client.Secrets = new List<OAuthClientSecret>();
                    }
                    oauthDownParty.Client.Secrets.Add(secret);
                }
                secretRequest.Secrets = oauthDownParty.Client.Secrets.Select(s => s.Id).ToList();
                if (!await ModelState.TryValidateObjectAsync(secretRequest)) return BadRequest(ModelState);
                await tenantRepository.UpdateAsync(oauthDownParty);

                return Created(new Api.OAuthDownParty { Name = secretRequest.PartyName });
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create secret on client '{typeof(TParty).Name}' by name '{secretRequest.PartyName}'.");
                    return Conflict(typeof(TParty).Name, secretRequest.PartyName, nameof(secretRequest.PartyName));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete a client secret.
        /// </summary>
        /// <param name="name">Name is [down-party name].[secret id] </param>
        protected async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);

                var partyName = name?.ToLower().GetFirstInDotList();
                if (!ModelState.TryValidateRequiredParameter(partyName, $"{nameof(name)}[0]")) return BadRequest(ModelState);
                var secretId = name.GetLastInDotList();
                if (!ModelState.TryValidateRequiredParameter(secretId, $"{nameof(name)}[1]")) return BadRequest(ModelState);

                var oauthDownParty = await tenantRepository.GetAsync<TParty>(await DownParty.IdFormatAsync(RouteBinding, partyName));
                var secret = oauthDownParty.Client.Secrets.Where(s => s.Id == secretId).FirstOrDefault();
                if (secret == null)
                {
                    return NotFound("Secret", secretId);
                }
                oauthDownParty.Client.Secrets.Remove(secret);
                await tenantRepository.UpdateAsync(oauthDownParty);

                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete secret from client '{typeof(TParty).Name}' by name '{name}'.");
                    return NotFound(typeof(TParty).Name, name);
                }
                throw;
            }
        }
    }
}
