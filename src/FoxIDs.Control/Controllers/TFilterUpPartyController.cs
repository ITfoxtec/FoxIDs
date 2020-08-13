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
    public class TFilterUpPartyController : TenantApiController
    {
        private const string dataType = "party:up";
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;

        public TFilterUpPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
        }

        /// <summary>
        /// Filter up party.
        /// </summary>
        /// <param name="filterName">Filter up party name.</param>
        /// <returns>Up party.</returns>
        [ProducesResponseType(typeof(HashSet<Api.UpParty>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.UpParty>>> GetFilterUpParty(string filterName)
        {
            try
            {
                var doFilterPartyType = Enum.TryParse<PartyTypes>(filterName, out var filterPartyType);
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mUpPartys = filterName.IsNullOrWhiteSpace() ? await tenantService.GetListAsync<UpParty>(idKey, whereQuery: p => p.DataType.Equals(dataType)) : await tenantService.GetListAsync<UpParty>(idKey, whereQuery: p => p.DataType.Equals(dataType) && (p.Name.Contains(filterName) || (doFilterPartyType && p.Type == filterPartyType)));
                var aUpPartys = new HashSet<Api.UpParty>(mUpPartys.Count());
                foreach(var mUpParty in mUpPartys.OrderBy(p => p.Name))
                {
                    aUpPartys.Add(mapper.Map<Api.UpParty>(mUpParty));
                }
                return Ok(aUpPartys);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.UpParty).Name}' by filter name '{filterName}'.");
                    return NotFound(typeof(Api.UpParty).Name, filterName);
                }
                throw;
            }
        }
    }
}
