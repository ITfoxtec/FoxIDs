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

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public class TFilterDownPartyController : ApiController
    {
        private const string dataType = Constants.Models.DataType.DownParty;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TFilterDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Filter application registration.
        /// </summary>
        /// <param name="filterName">Filter application registration name.</param>
        /// <returns>Application registrations.</returns>
        [ProducesResponseType(typeof(HashSet<Api.DownParty>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.DownParty>>> GetFilterDownParty(string filterName)
        {
            try
            {
                var doFilterPartyType = Enum.TryParse<PartyTypes>(filterName, out var filterPartyType);
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mDownPartys, _) = filterName.IsNullOrWhiteSpace() ? 
                    await tenantDataRepository.GetListAsync<DownParty>(idKey, whereQuery: p => p.DataType.Equals(dataType)) : 
                    await tenantDataRepository.GetListAsync<DownParty>(idKey, whereQuery: p => p.DataType.Equals(dataType) && 
                        (p.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase) || p.DisplayName.Contains(filterName, StringComparison.OrdinalIgnoreCase) || (doFilterPartyType && p.Type == filterPartyType)));

                var aDownPartys = new HashSet<Api.DownParty>(mDownPartys.Count());
                foreach(var mDownParty in mDownPartys.OrderBy(p => p.Type).ThenBy(p => p.Name))
                {
                    aDownPartys.Add(mapper.Map<Api.DownParty>(mDownParty));
                }
                return Ok(aDownPartys);
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
