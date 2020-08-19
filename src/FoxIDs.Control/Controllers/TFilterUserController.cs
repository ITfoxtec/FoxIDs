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
    public class TFilterUserController : TenantApiController
    {
        private const string dataType = "user";
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantRepository tenantService;

        public TFilterUserController(TelemetryScopedLogger logger, IMapper mapper, ITenantRepository tenantService) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantService = tenantService;
        }

        /// <summary>
        /// Filter up party.
        /// </summary>
        /// <param name="filterEmail">Filter user email.</param>
        /// <returns>Users.</returns>
        [ProducesResponseType(typeof(HashSet<Api.User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.User>>> GetFilterUser(string filterEmail)
        {
            try
            {
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName, };
                var mUsers = filterEmail.IsNullOrWhiteSpace() ? await tenantService.GetListAsync<User>(idKey, whereQuery: u => u.DataType.Equals(dataType)) : await tenantService.GetListAsync<User>(idKey, whereQuery: u => u.DataType.Equals(dataType) && u.Email.Contains(filterEmail));
                var aUsers = new HashSet<Api.User>(mUsers.Count());
                foreach(var mUser in mUsers.OrderBy(t => t.Email))
                {
                    aUsers.Add(mapper.Map<Api.User>(mUser));
                }
                return Ok(aUsers);
            }
            catch (CosmosDataException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.User).Name}' by filter email '{filterEmail}'.");
                    return NotFound(typeof(Api.User).Name, filterEmail);
                }
                throw;
            }
        }
    }
}
