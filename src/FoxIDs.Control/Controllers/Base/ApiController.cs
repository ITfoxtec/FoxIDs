using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Log]
    [HttpSecurityHeaders]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public abstract class ApiController : ControllerBase, IRouteBinding
    {
        private readonly TelemetryScopedLogger logger;

        public ApiController(TelemetryScopedLogger logger)
        {
            this.logger = logger;
        }

        public RouteBinding RouteBinding => HttpContext.GetRouteBinding();

        public virtual CreatedResult Created(INameValue value)
        {
            return Created(new { name = value.Name }, value);
        }
        public virtual CreatedResult Created(IEmailValue value)
        {
            return Created(new { name = value.Email }, value);
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

        public virtual NotFoundObjectResult NotFound(string typeName, string name, string fieldName = null)
        {
            try
            {
                throw new Exception($"{typeName} '{name}' not found.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
                if (!fieldName.IsNullOrWhiteSpace())
                {
                    Response.Headers["field"] = fieldName;
                }
                return base.NotFound(ex.Message);
            }
        }

        public virtual ConflictObjectResult Conflict(string typeName, string name, string fieldName = null)
        {
            try
            {
                throw new Exception($"{typeName} '{name}' already exists.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex);
                if (!fieldName.IsNullOrWhiteSpace())
                {
                    Response.Headers["field"] = fieldName;
                }
                return base.Conflict(ex.Message);
            }
        }

        public BadRequestObjectResult BadRequest(ModelStateDictionary modelState, Exception innerEx = null)
        {
            try
            {
                var errors = modelState.Values.SelectMany(v => v.Errors.Select(e => $"{e.ErrorMessage}{(e.Exception != null ? $", {e.Exception}" : string.Empty)}"));
                throw new Exception(string.Join("; ", errors), innerEx);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                if (modelState.Keys.Count() == 1)
                {
                    Response.Headers["field"] = modelState.Keys.First();
                }
                return base.BadRequest(ex.Message);
            }
        }
    }
}
