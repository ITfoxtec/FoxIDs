using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    public class TOAuthDownPartyController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantService;

        public TOAuthDownPartyController(TelemetryScopedLogger logger, ITenantRepository tenantService) : base(logger)
        {
            this.logger = logger;
            this.tenantService = tenantService;
        }

        [ProducesResponseType(typeof(OAuthDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OAuthDownParty>> GetOAuthDownParty(string id)
        {
            try
            {
                var oauthDownParty = await tenantService.GetAsync<OAuthDownParty>(await GetDownPartyIdKeyFromId(id));
                return oauthDownParty;
            }
            catch (CosmosDataException ex)
            {
                if(ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Get by id '{id}'.");
                    return NotFound();
                }
                throw;
            }
        }

        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> PostOAuthDownParty([FromBody] OAuthDownParty oauthDownParty)
        {
            //var msd = new ModelStateDictionary();
            //msd.

            // IValidatableObject
            // https://docs.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-2.2#ivalidatableobject

            if (!ModelState.IsValid) return BadRequest(ModelState);

            oauthDownParty.Id = await GetDownPartyIdKeyFromId(oauthDownParty.Id);
            TryValidateModel(oauthDownParty);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await tenantService.SaveAsync(oauthDownParty);
            return CreatedAtAction(nameof(GetOAuthDownParty), new { id = oauthDownParty.Id }, oauthDownParty);
        }

        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOAuthDownParty(string id)
        {
            try
            {
                await tenantService.DeleteAsync<OAuthDownParty>(await GetDownPartyIdKeyFromId(id));
                return NoContent();
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"Delete by id '{id}'.");
                    return NotFound();
                }
                throw;
            }
        }

        private async Task<string> GetDownPartyIdKeyFromId(string id)
        {
            var partyName = id.Substring(id.LastIndexOf(':') + 1);
            var party = new Party.IdKey
            {
                TenantName = RouteBinding.TenantName,
                TrackName = RouteBinding.TrackName,
                PartyName = partyName,
            };
            TryValidateModel(party);//.ValidateObjectAsync();

            return DownParty.IdFormat(party);
        }
    }
}
