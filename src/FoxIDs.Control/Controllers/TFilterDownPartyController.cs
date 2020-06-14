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

namespace FoxIDs.Controllers
{
    public class TFilterDownPartyController : TenantApiController
    {
        private const string dataType = "party:down";
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;

        public TFilterDownPartyController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
        }

        /// <summary>
        /// Filter down party.
        /// </summary>
        /// <param name="filterName">Filter down party name.</param>
        /// <returns>Down party.</returns>
        [ProducesResponseType(typeof(HashSet<Api.DownParty>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.DownParty>>> GetFilterDownParty(string filterName)
        {
            try
            {
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                var mDownPartys = filterName.IsNullOrWhiteSpace() ? await tenantService.GetListAsync<DownParty>(idKey, whereQuery: p => p.DataType.Equals(dataType)) : await tenantService.GetListAsync<DownParty>(idKey, whereQuery: p => p.DataType.Equals(dataType) && p.Name.Contains(filterName));
                var aDownPartys = new HashSet<Api.DownParty>(mDownPartys.Count());
                foreach(var mDownParty in mDownPartys)
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
