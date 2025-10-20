using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using System;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public class TDownPartiesController : ApiController
    {
        private const string dataType = Constants.Models.DataType.DownParty;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PartyLogic partyLogic;

        public TDownPartiesController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.partyLogic = partyLogic;
        }

        /// <summary>
        /// Get application registrations.
        /// </summary>
        /// <param name="filterName">Filter application registration name.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Application registrations.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.DownParty>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.DownParty>>> GetDownParties(string filterName, string paginationToken = null)
        {
            await partyLogic.DeleteExporedDownParties();

            try
            {
                var doFilterPartyType = Enum.TryParse<PartyTypes>(filterName, out var filterPartyType);
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mDownPartys, var nextPaginationToken) = filterName.IsNullOrWhiteSpace() ? 
                    await tenantDataRepository.GetManyAsync<DownParty>(idKey, whereQuery: p => p.DataType.Equals(dataType), paginationToken: paginationToken) : 
                    await tenantDataRepository.GetManyAsync<DownParty>(idKey, whereQuery: p => p.DataType.Equals(dataType) && 
                        (p.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || p.DisplayName.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || (doFilterPartyType && p.Type == filterPartyType)), paginationToken: paginationToken);

                var response = new Api.PaginationResponse<Api.DownParty>
                {
                    Data = new HashSet<Api.DownParty>(mDownPartys.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach(var mDownParty in mDownPartys.OrderBy(p => p.DisplayName ?? p.Name).ThenBy(p => p.Type))
                {
                    response.Data.Add(mapper.Map<Api.DownParty>(mDownParty));
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.DownParty).Name}' by filter name '{filterName}'.");
                    return NotFound(typeof(Api.DownParty).Name, filterName);
                }
                throw;
            }
        }
    }
}
