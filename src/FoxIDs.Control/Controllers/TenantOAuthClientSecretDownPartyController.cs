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

namespace FoxIDs.Controllers
{
    /// <summary>
    /// Abstract OAuth 2.0 client secret for down party api.
    /// </summary>
    public abstract class TenantClientSecretDownPartyController<TParty, TClient, TScope, TClaim> : TenantApiController where TParty : OAuthDownParty<TClient, TScope, TClaim> where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;
        private readonly SecretHashLogic secretHashLogic;

        public TenantClientSecretDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService, SecretHashLogic secretHashLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
            this.secretHashLogic = secretHashLogic;
        }

        protected async Task<ActionResult<Api.OAuthClientSecretResponse>> Get(string partyName)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(partyName, nameof(partyName))) return BadRequest(ModelState);

                var oauthDownParty = await tenantService.GetAsync<TParty>(await DownParty.IdFormat(RouteBinding, partyName));
                return Ok(mapper.Map<List<Api.OAuthClientSecretResponse>>(oauthDownParty.Client.Secrets).Select(c => { c.Name = new[] { partyName, c.Name }.ToDotList(); return c; }));
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

        protected async Task<ActionResult<Api.OAuthClientSecretResponse>> Post(Api.OAuthClientSecretRequest party)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(party)) return BadRequest(ModelState);

                var oauthDownParty = await tenantService.GetAsync<TParty>(await DownParty.IdFormat(RouteBinding, party.PartyName));

                var secret = new OAuthClientSecret();
                await secretHashLogic.AddSecretHashAsync(secret, party.Secret);
                if(oauthDownParty.Client.Secrets == null)
                {
                    oauthDownParty.Client.Secrets = new List<OAuthClientSecret>();
                }
                oauthDownParty.Client.Secrets.Add(secret);
                await tenantService.UpdateAsync(oauthDownParty);

                return Created(mapper.Map<Api.OAuthClientSecretResponse>(secret).Set(s => s.Name = new[] { oauthDownParty.Name, s.Name }.ToDotList()));
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create secret on client '{typeof(TParty).Name}' by name '{party.PartyName}'.");
                    return Conflict(typeof(TParty).Name, party.PartyName);
                }
                throw;
            }
        }

        protected async Task<IActionResult> Delete(string name)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(name, nameof(name))) return BadRequest(ModelState);

                var partyName = name.GetFirstInDotList();
                var secretId = name.GetLastInDotList();
                var oauthDownParty = await tenantService.GetAsync<TParty>(await DownParty.IdFormat(RouteBinding, partyName));
                var secret = oauthDownParty.Client.Secrets.Where(s => s.Id == secretId).FirstOrDefault();
                if (secret == null)
                {
                    return NotFound("Secret", secretId);
                }
                oauthDownParty.Client.Secrets.Remove(secret);
                await tenantService.UpdateAsync(oauthDownParty);

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
