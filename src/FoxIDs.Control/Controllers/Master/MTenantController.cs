//using AutoMapper;
//using FoxIDs.Infrastructure;
//using FoxIDs.Repository;
//using FoxIDs.Models;
//using Api = FoxIDs.Models.Api;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using System.Threading.Tasks;
//using System.Net;

//namespace FoxIDs.Controllers.Master
//{
//    public class MTenantController : MasterApiController
//    {
//        private readonly TelemetryScopedLogger logger;
//        private readonly IMapper mapper;
//        private readonly ITenantRepository tenantService;

//        public MTenantController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService) : base(logger)
//        {
//            this.logger = logger;
//            this.mapper = mapper;
//            this.tenantService = tenantService;
//        }

//        /// <summary>
//        /// Create tenant.
//        /// </summary>
//        /// <param name="tenant">Tenant.</param>
//        /// <returns>Tenant.</returns>
//        [ProducesResponseType(typeof(Api.OAuthClientSecretResponse), StatusCodes.Status201Created)]
//        [ProducesResponseType(StatusCodes.Status204NoContent)]
//        public async Task<ActionResult<Api.Tenant>> PostTenant([FromBody] Api.Tenant tenant)
//        {
//            try
//            {
//                if (!await ModelState.TryValidateObjectAsync(tenant)) return BadRequest(ModelState);

//                var mTenant = mapper.Map<Tenant>(tenant);
//                await tenantService.CreateAsync(mTenant);

//                return Created(mapper.Map<Api.Tenant>(mTenant));
//            }
//            catch (CosmosDataException ex)
//            {
//                if (ex.StatusCode == HttpStatusCode.Conflict)
//                {
//                    logger.Warning(ex, $"Conflict, Create '{typeof(Api.Tenant).Name}' by name '{tenant.Name}'.");
//                    return Conflict(typeof(Api.Tenant).Name, tenant.Name);
//                }
//                throw;
//            }
//        }
//    }
//}
