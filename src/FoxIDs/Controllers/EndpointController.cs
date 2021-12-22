using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [FoxIDsHttpSecurityHeaders]
    public abstract class EndpointController : Controller, IRouteBinding
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
            if (RouteBinding == null) throw new InvalidOperationException("Controller can not be called directly.");

            logger.ScopeTrace(() => $"Url '{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{context.HttpContext.Request.Path}'");
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result.IsHtmlContent(typeof(ErrorViewModel)) && RouteBinding.Key.PrimaryKey.ExternalKeyIsNotReady)
            {
                throw new ExternalKeyIsNotReadyException("Primary external track key certificate is not ready in Key Vault.");
            }

            base.OnActionExecuted(context);
        }
    }
}
