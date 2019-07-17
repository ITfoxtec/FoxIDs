using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpSecurityHeaders]
    public abstract class EndpointController : Controller
    {
        private readonly TelemetryScopedLogger logger;

        public EndpointController(TelemetryScopedLogger logger)
        {
            this.logger = logger;
        }

        public RouteBinding RouteBinding => HttpContext.GetRouteBinding();
        
        public Sequence Sequence => HttpContext.GetSequence();

        public string SequenceString => HttpContext.GetSequenceString();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            logger.ScopeTrace($"Url '{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{context.HttpContext.Request.Path}'");
            base.OnActionExecuting(context);
        }
    }
}
