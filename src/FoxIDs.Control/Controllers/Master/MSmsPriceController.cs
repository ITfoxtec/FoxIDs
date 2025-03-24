using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AutoMapper;
using System.Linq;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using System.Collections.Generic;
using System;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
    [MasterScopeAuthorize]
    public class MSmsPriceController : ApiController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly IMasterDataRepository masterDataRepository;

        public MSmsPriceController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, IMasterDataRepository masterDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.masterDataRepository = masterDataRepository;
        }

        /// <summary>
        /// Get SMS price.
        /// </summary>
        /// <param name="iso2">SMS price ISO2.</param>
        /// <returns>SMS price.</returns>
        [ProducesResponseType(typeof(Api.SmsPrice), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SmsPrice>> GetSmsPrice(string iso2)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(iso2, nameof(iso2))) return BadRequest(ModelState);
                iso2 = iso2?.ToUpper();

                (var mSmsPrices, _) = await GetSmsPricesAsync();
                var mSmsPrice = mSmsPrices.Countries.Where(c => c.Iso2 == iso2).FirstOrDefault();
                if (mSmsPrice == null)
                {
                    throw new FoxIDsDataException { StatusCode = DataStatusCode.NotFound };
                }

                return Ok(mapper.Map<Api.SmsPrice>(mSmsPrice));
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.SmsPrice).Name}' by ISO2 '{iso2}'.");
                    return NotFound(typeof(Api.SmsPrice).Name, iso2);
                }
                throw;
            }
        }

        /// <summary>
        /// Create SMS price.
        /// </summary>
        /// <param name="smsPrice">SMS price.</param>
        [ProducesResponseType(typeof(Api.SmsPrice), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> PostSmsPrice([FromBody] Api.SmsPrice smsPrice)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(smsPrice)) return BadRequest(ModelState);
                smsPrice.Iso2 = smsPrice.Iso2.Trim().ToUpper();

                (var mSmsPrices, var createSmsPrices) = await GetSmsPricesAsync();
                if (mSmsPrices.Countries.Where(c => c.Iso2 == smsPrice.Iso2).Any())
                {
                    throw new FoxIDsDataException { StatusCode = DataStatusCode.Conflict };
                }
                if (mSmsPrices.Countries.Where(c => c.PhoneCode == smsPrice.PhoneCode).Any())
                {
                    throw new Exception($"SMS price with phone code '{smsPrice.PhoneCode}' already exist.");
                }

                mSmsPrices.Countries.Add(mapper.Map<SmsPrice>(smsPrice));

                await SaveSmsPricesAsync(mSmsPrices, createSmsPrices);

                return Created();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.SmsPrice).Name}' by ISO2 '{smsPrice.Iso2}'.");
                    return Conflict(typeof(Api.SmsPrice).Name, smsPrice.Iso2, nameof(smsPrice.Iso2));
                }
                throw;
            }
        }

        /// <summary>
        /// Update SMS price.
        /// </summary>
        /// <param name="smsPrice">SMS price.</param>
        [ProducesResponseType(typeof(Api.SmsPrice), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Api.SmsPrice>> PutSmsPrice([FromBody] Api.SmsPrice smsPrice)
        {
            try
            {
                if (!await ModelState.TryValidateObjectAsync(smsPrice)) return BadRequest(ModelState);
                smsPrice.Iso2 = smsPrice.Iso2.Trim().ToUpper();

                (var mSmsPrices, var createSmsPrices) = await GetSmsPricesAsync();
                var mSmsPrice = mSmsPrices.Countries.Where(c => c.Iso2 == smsPrice.Iso2).FirstOrDefault();
                if (mSmsPrice == null)
                {
                    throw new FoxIDsDataException { StatusCode = DataStatusCode.NotFound };
                }
                if (mSmsPrices.Countries.Where(c => c.Iso2 != smsPrice.Iso2 && c.PhoneCode == smsPrice.PhoneCode).Any())
                {
                    throw new Exception($"SMS price with phone code '{smsPrice.PhoneCode}' already exist.");
                }

                mSmsPrices.Countries = mSmsPrices.Countries.Where(c => c.Iso2 != smsPrice.Iso2).ToList();
                mSmsPrices.Countries.Add(mapper.Map<SmsPrice>(smsPrice));

                await SaveSmsPricesAsync(mSmsPrices, createSmsPrices);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(Api.SmsPrice).Name}' by ISO2 '{smsPrice.Iso2}'.");
                    return NotFound(typeof(Api.SmsPrice).Name, smsPrice.Iso2, nameof(smsPrice.Iso2));
                }
                throw;
            }
        }

        /// <summary>
        /// Delete SMS price.
        /// </summary>
        /// <param name="iso2">SMS price ISO2.</param>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSmsPrice(string iso2)
        {
            try
            {
                if (!ModelState.TryValidateRequiredParameter(iso2, nameof(iso2))) return BadRequest(ModelState);
                iso2 = iso2?.ToUpper();

                (var mSmsPrices, var createSmsPrices) = await GetSmsPricesAsync();                
                if (!mSmsPrices.Countries.Where(c => c.Iso2 == iso2).Any())
                {
                    throw new FoxIDsDataException { StatusCode = DataStatusCode.NotFound };
                }

                mSmsPrices.Countries = mSmsPrices.Countries.Where(c => c.Iso2 != iso2).ToList();

                await SaveSmsPricesAsync(mSmsPrices, createSmsPrices);

                return NoContent();
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Delete '{typeof(Api.SmsPrice).Name}' by name '{iso2}'.");
                    return NotFound(typeof(Api.SmsPrice).Name, iso2);
                }
                throw;
            }
        }

        private async Task<(SmsPrices mSmsPrices, bool createSmsPrices)> GetSmsPricesAsync()
        {
            var createSmsPrices = false;
            var mSmsPrices = await masterDataRepository.GetAsync<SmsPrices>(await SmsPrices.IdFormatAsync(), required: false);
            if (mSmsPrices == null)
            {
                mSmsPrices = new SmsPrices
                {
                    Id = await SmsPrices.IdFormatAsync()
                };
                createSmsPrices = true;
            }

            if (mSmsPrices.Countries == null)
            {
                mSmsPrices.Countries = new List<SmsPrice>();
            }

            return (mSmsPrices, createSmsPrices);
        }

        private async Task SaveSmsPricesAsync(SmsPrices mSmsPrices, bool createSmsPrices)
        {
            mSmsPrices.Countries = mSmsPrices.Countries.OrderBy(c => c.Iso2).ToList();
            if (createSmsPrices)
            {
                await masterDataRepository.CreateAsync(mSmsPrices);
            }
            else
            {
                await masterDataRepository.UpdateAsync(mSmsPrices);
            }
        }
    }
}
