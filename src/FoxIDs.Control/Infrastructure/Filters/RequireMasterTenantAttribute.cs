using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireMasterTenantAttribute : TypeFilterAttribute
    {
        public RequireMasterTenantAttribute() : base(typeof(RequireMasterTenantActionAttribute))
        { }

        private class RequireMasterTenantActionAttribute : IAsyncActionFilter
        {
            private readonly TelemetryScopedLogger logger;

            public RequireMasterTenantActionAttribute(TelemetryScopedLogger logger)
            {
                this.logger = logger;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var routeBinding = context.HttpContext.GetRouteBinding();

                try
                {
                    if (!routeBinding.TenantName.Equals(Constants.Routes.MasterTenantName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new UnauthorizedAccessException("API require master tenant.");
                    }

                    await next();
                }
                catch (UnauthorizedAccessException uaex)
                {
                    logger.Warning(uaex);
                    context.Result = new UnauthorizedResult();
                }
            }
        }
    }
}