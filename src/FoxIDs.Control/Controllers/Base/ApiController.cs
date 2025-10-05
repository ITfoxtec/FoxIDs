using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Log]
    [HttpSecurityHeaders]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public abstract class ApiController : ControllerBase, IRouteBinding, IAsyncActionFilter
    {
        private readonly TelemetryScopedLogger logger;

        private bool auditLogEnabled;

        public ApiController(TelemetryScopedLogger logger, bool auditLogEnabled = true)
        {
            this.logger = logger;
            this.auditLogEnabled = auditLogEnabled;
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

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (auditLogEnabled)
            {
                var requestMethod = context.HttpContext.Request.Method;
                var shouldAudit = HttpMethods.IsPost(requestMethod) || HttpMethods.IsPut(requestMethod) || HttpMethods.IsDelete(requestMethod);
                if (shouldAudit)
                {
                    context.HttpContext.Items[Constants.ControlApi.AuditLogEnabledKey] = true;
                }
            }

            await next();
        }

        public virtual NotFoundObjectResult NotFound(string typeName, string name, string fieldName = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                throw new Exception($"{typeName} '{name}' not found.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, GetMessage("Not found", memberName, sourceFilePath, sourceLineNumber));
                if (!fieldName.IsNullOrWhiteSpace())
                {
                    Response.Headers["field"] = fieldName;
                }
                return base.NotFound(ex.Message);
            }
        }

        public virtual ConflictObjectResult Conflict(string typeName, string name, string fieldName = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                throw new Exception($"{typeName} '{name}' already exists.");
            }
            catch (Exception ex)
            {
                logger.Warning(ex, GetMessage("Conflict", memberName, sourceFilePath, sourceLineNumber));
                if (!fieldName.IsNullOrWhiteSpace())
                {
                    Response.Headers["field"] = fieldName;
                }
                return base.Conflict(ex.Message);
            }
        }

        public BadRequestObjectResult BadRequest(ModelStateDictionary modelState, Exception innerEx = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                var errors = modelState.Values.SelectMany(v => v.Errors.Select(e => $"{e.ErrorMessage}{(e.Exception != null ? $", {e.Exception}" : string.Empty)}"));
                throw new Exception(string.Join("; ", errors), innerEx);
            }
            catch (Exception ex)
            {
                logger.Error(ex, GetMessage("Bad request", memberName, sourceFilePath, sourceLineNumber));
                if (modelState.Keys.Count() == 1)
                {
                    Response.Headers["field"] = modelState.Keys.First();
                }
                return base.BadRequest(ex.Message);
            }
        }

        private string GetMessage(string message, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            return $"{message} at {memberName} in {sourceFilePath}:line {sourceLineNumber}";
        }
    }
}
