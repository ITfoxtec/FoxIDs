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
using FoxIDs.Models.SearchModels;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.Party)]
    public class TUpPartiesController : ApiController
    {
        private const string dataType = Constants.Models.DataType.UpParty;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TUpPartiesController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Get authentication methods.
        /// </summary>
        /// <param name="filterName">Filter authentication method by name.</param>
        /// <param name="filterHrdDomains">Filter authentication method by HRD domains.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>Authentication methods.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.UpParty>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Api.PaginationResponse<Api.UpParty>>> GetUpParties(string filterName, string filterHrdDomains, string paginationToken = null)
        {
            try
            {
                filterName = filterName?.Trim();
                filterHrdDomains = filterHrdDomains?.Trim();
                (var mUpPartys, var nextPaginationToken) = await GetFilterUpPartyInternalAsync(filterName, filterHrdDomains, paginationToken);

                var response = new Api.PaginationResponse<Api.UpParty>
                {
                    Data = new HashSet<Api.UpParty>(mUpPartys.Count()),
                    PaginationToken = nextPaginationToken,
                };
                foreach (var mUpParty in mUpPartys.OrderBy(p => p.DisplayName ?? p.Name).ThenBy(p => p.Type))
                {
                    response.Data.Add(mapper.Map<Api.UpParty>(mUpParty));
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.UpParty).Name}' by filter name '{filterName}'.");
                    return NotFound(typeof(Api.UpParty).Name, filterName);
                }
                throw;
            }
        }

        private async Task<(IReadOnlyCollection<UpPartyWithProfile<UpPartyProfile>> items, string paginationToken)> GetFilterUpPartyInternalAsync(string filterName, string filterHrdDomains, string paginationToken)
        {
            var doFilterPartyType = Enum.TryParse<PartyTypes>(filterName, out var filterPartyType);
            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };

            if (filterName.IsNullOrWhiteSpace() && filterHrdDomains.IsNullOrWhiteSpace())
            {
                return await tenantDataRepository.GetManyAsync<SearchUpPartyWithProfile<UpPartyProfile>>(idKey, whereQuery: p => p.DataType.Equals(dataType), paginationToken: paginationToken);
            }
            else if(!filterName.IsNullOrWhiteSpace() && filterHrdDomains.IsNullOrWhiteSpace())
            {
                return await tenantDataRepository.GetManyAsync<SearchUpPartyWithProfile<UpPartyProfile>>(idKey, whereQuery: p => p.DataType.Equals(dataType) &&
                    (p.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || p.DisplayName.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) ||
                      (p.Client != null && p.Client.SpClientId.Contains(filterName, StringComparison.CurrentCultureIgnoreCase)) ||
                      (p.Profiles != null && p.Profiles.Any(p => p.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || p.DisplayName.Contains(filterName, StringComparison.CurrentCultureIgnoreCase))) ||
                      (doFilterPartyType && p.Type == filterPartyType)), paginationToken: paginationToken);
            }
            else if (filterName.IsNullOrWhiteSpace() && !filterHrdDomains.IsNullOrWhiteSpace())
            {
                return await tenantDataRepository.GetManyAsync<SearchUpPartyWithProfile<UpPartyProfile>>(idKey, whereQuery: p => p.DataType.Equals(dataType) &&
                    p.HrdDomains.Where(d => d.Contains(filterHrdDomains, StringComparison.CurrentCultureIgnoreCase)).Any(), paginationToken: paginationToken);
            }
            else
            {
                return await tenantDataRepository.GetManyAsync<SearchUpPartyWithProfile<UpPartyProfile>>(idKey, whereQuery: p => p.DataType.Equals(dataType) &&
                    (p.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || p.DisplayName.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) ||
                      (p.Client != null && p.Client.SpClientId.Contains(filterName, StringComparison.CurrentCultureIgnoreCase)) ||
                      (p.Profiles != null && p.Profiles.Any(p => p.Name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase) || p.DisplayName.Contains(filterName, StringComparison.CurrentCultureIgnoreCase))) ||
                      (doFilterPartyType && p.Type == filterPartyType)) ||
                      p.HrdDomains.Where(d => d.Contains(filterHrdDomains, StringComparison.CurrentCultureIgnoreCase)).Any(), paginationToken: paginationToken);
            }
        }
    }
}
