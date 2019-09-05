using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    //[ApiExplorerSettings(GroupName = "OAuthDownParty")]
    [Route("(tenant)/(track)/!oauthdownparty")]
    //[ApiExplorerSettings( GroupName = nameof(TOAuthDownPartyController))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class TOAuthDownPartyController : TenantApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantService;

        public TOAuthDownPartyController(TelemetryScopedLogger logger, ITenantRepository tenantService) : base(logger)
        {
            this.logger = logger;
            this.tenantService = tenantService;
        }


        //[SwaggerOperation("test")]
        [HttpGet("id")]
        [ProducesResponseType(typeof(OAuthDownParty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OAuthDownParty>> Get(string id)
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Post([FromBody] OAuthDownParty oauthDownParty)
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
            return CreatedAtAction(nameof(Get), new { id = oauthDownParty.Id }, oauthDownParty);
        }

        [HttpDelete("id")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
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
