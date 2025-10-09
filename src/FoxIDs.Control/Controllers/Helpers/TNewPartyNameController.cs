using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Basic, Constants.ControlApi.Segment.Party)]
    public class TNewPartyNameController : ApiController
    {
        private readonly PartyLogic partyLogic;

        public TNewPartyNameController(TelemetryScopedLogger logger, PartyLogic partyLogic) : base(logger, auditLogEnabled: false)
        {
            this.partyLogic = partyLogic;
        }

        /// <summary>
        /// Get new unique party name.
        /// </summary>
        /// <returns>Client settings.</returns>
        [ProducesResponseType(typeof(Api.NewPartyName), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.NewPartyName>> GetNewPartyName(bool isUpParty = false)
        {
            return Ok(new Api.NewPartyName
            {
                Name = await partyLogic.GeneratePartyNameAsync(isUpParty),
                IsUpParty = isUpParty,
            });
        }
    }
}
