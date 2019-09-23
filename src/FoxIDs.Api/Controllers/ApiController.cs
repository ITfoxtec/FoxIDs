using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;

namespace FoxIDs.Controllers
{
    [HttpSecurityHeaders]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public abstract class ApiController : ControllerBase
    {
        public RouteBinding RouteBinding => HttpContext.GetRouteBinding();

        public virtual CreatedResult Created(INameValue value)
        {
            return Created(new { name = value.Name }, value);
        }

        public virtual CreatedResult Created(object queryValues, object value)
        {
            var routeValues = new RouteValueDictionary(queryValues).Select(r => $"{r.Key}={r.Value}");
            var uriBuilder = new UriBuilder(new Uri(HttpContext.GetHostUri(), Request.Path))
            {
                Query = string.Join('&', routeValues)
            };
            return new CreatedResult(uriBuilder.Uri, value);
        }

        public virtual NotFoundObjectResult NotFound(string typeName, string name)
        {
            return NotFound($"{typeName} '{name}' not found.");
        }

        public virtual ConflictObjectResult Conflict(string typeName, string name)
        {
            return Conflict($"{typeName} '{name}' already exists.");
        }
    }
}
