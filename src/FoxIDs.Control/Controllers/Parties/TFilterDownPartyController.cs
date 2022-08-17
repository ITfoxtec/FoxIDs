using AutoMapper;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using System;

namespace FoxIDs.Controllers
{
    public class TFilterDownPartyController : TenantApiController
    {
        private const string dataType = "party:down";
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantRepository;

        public TFilterDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Filter down-party.
        /// </summary>
        /// <param name="filterName">Filter down-party name.</param>
        /// <returns>Down-party.</returns>
        [ProducesResponseType(typeof(HashSet<Api.DownParty>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.DownParty>>> GetFilterDownParty(string filterName)
        {
            try
            {
                var doFilterPartyType = Enum.TryParse<PartyTypes>(filterName, out var filterPartyType);
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mDownPartys, _) = filterName.IsNullOrWhiteSpace() ? await tenantRepository.GetListAsync<DownParty>(idKey, whereQuery: p => p.DataType.Equals(dataType)) : await tenantRepository.GetListAsync<DownParty>(idKey, whereQuery: p => p.DataType.Equals(dataType) && (p.Name.Contains(filterName) || (doFilterPartyType && p.Type == filterPartyType)));
                var aDownPartys = new HashSet<Api.DownParty>(mDownPartys.Count());
                foreach(var mDownParty in mDownPartys.OrderBy(p => p.Name))
                {
                    aDownPartys.Add(mapper.Map<Api.DownParty>(mDownParty));
                }
                return Ok(aDownPartys);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.DownParty).Name}' by filter name '{filterName}'.");
                    return NotFound(typeof(Api.DownParty).Name, filterName);
                }
                throw;
            }
        }
    }
}
