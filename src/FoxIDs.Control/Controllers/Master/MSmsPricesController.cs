using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using System.Linq;
using ITfoxtec.Identity;
using System;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]

    public class MSmsPricesController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly IMasterDataRepository masterDataRepository;

        public MSmsPricesController(TelemetryScopedLogger logger, IMapper mapper, IMasterDataRepository masterDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.masterDataRepository = masterDataRepository;
        }

        /// <summary>
        /// Get SMS prices.
        /// </summary>
        /// <param name="filterName">Filter SMS price by country name and ISO2.</param>
        /// <param name="paginationToken">The pagination token.</param>
        /// <returns>SMS prices.</returns>
        [ProducesResponseType(typeof(Api.PaginationResponse<Api.SmsPrice>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.PaginationResponse<Api.SmsPrice>>> GetSmsPrices(string filterName, string paginationToken = null)
        {
            try
            {
                var mSmsPrices = await masterDataRepository.GetAsync<SmsPrices>(await SmsPrices.IdFormatAsync(), required: false);
                
                var response = new Api.PaginationResponse<Api.SmsPrice>
                {
                    Data = new HashSet<Api.SmsPrice>(mSmsPrices?.Countries?.Count ?? 0),
                };
                if (mSmsPrices != null && mSmsPrices.Countries != null)
                {
                    foreach (var mSmsPrice in mSmsPrices.Countries.Where(c => filterName.IsNullOrWhiteSpace() || c.CountryName.Contains(filterName, StringComparison.InvariantCultureIgnoreCase) || c.Iso2.Contains(filterName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        response.Data.Add(mapper.Map<Api.SmsPrice>(mSmsPrice));
                    }
                }
                return Ok(response);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.SmsPrice).Name}' by filter country name and ISO2 '{filterName}'.");
                    return NotFound(typeof(Api.SmsPrice).Name, filterName);
                }
                throw;
            }
        }
    }
}
