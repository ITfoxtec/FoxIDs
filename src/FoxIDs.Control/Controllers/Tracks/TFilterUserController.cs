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
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize(Constants.ControlApi.Segment.User)]
    public class TFilterUserController : ApiController
    {
        private const string dataType = Constants.Models.DataType.User;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;

        public TFilterUserController(TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository) : base(logger)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
        }

        /// <summary>
        /// Filter user.
        /// </summary>
        /// <param name="filterEmail">Filter user email.</param>
        /// <returns>Users.</returns>
        [ProducesResponseType(typeof(HashSet<Api.User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HashSet<Api.User>>> GetFilterUser(string filterEmail)
        {
            try
            {
                var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
                (var mUsers, _) = filterEmail.IsNullOrWhiteSpace() ? 
                    await tenantDataRepository.GetListAsync<User>(idKey, whereQuery: u => u.DataType.Equals(dataType)) : 
                    await tenantDataRepository.GetListAsync<User>(idKey, whereQuery: u => u.DataType.Equals(dataType) && 
                        u.Email.Contains(filterEmail, StringComparison.OrdinalIgnoreCase));
              
                var aUsers = new HashSet<Api.User>(mUsers.Count());
                foreach(var mUser in mUsers.OrderBy(t => t.Email))
                {
                    aUsers.Add(mapper.Map<Api.User>(mUser));
                }
                return Ok(aUsers);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Get '{typeof(Api.User).Name}' by filter email '{filterEmail}'.");
                    return NotFound(typeof(Api.User).Name, filterEmail);
                }
                throw;
            }
        }
    }
}
