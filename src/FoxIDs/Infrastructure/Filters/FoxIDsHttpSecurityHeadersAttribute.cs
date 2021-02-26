using FoxIDs.Logic;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class FoxIDsHttpSecurityHeadersAttribute : HttpSecurityHeadersAttribute
    {
        public FoxIDsHttpSecurityHeadersAttribute() : base(typeof(FoxIDsHttpSecurityHeadersActionAttribute))
        { }

        private class FoxIDsHttpSecurityHeadersActionAttribute : HttpSecurityHeadersActionAttribute
        {
            private List<string> allowFormActionOnDomains;
            private List<string> allowIframeOnDomains;
            private readonly IServiceProvider serviceProvider;

            public FoxIDsHttpSecurityHeadersActionAttribute(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IWebHostEnvironment env) : base(logger, env)
            {
                this.serviceProvider = serviceProvider;
            }

            protected override async Task ActionExecutionInitAsync(ActionExecutedContext resultContext)
            {
                await base.ActionExecutionInitAsync(resultContext);
                allowFormActionOnDomains = await GetFormActionOnDomainsAsync();
                allowIframeOnDomains = GetAllowIframeOnDomains(resultContext.Controller);
            }

            private async Task<List<string>> GetFormActionOnDomainsAsync()
            {
                var formActionLogic = serviceProvider.GetService<FormActionLogic>();
                return await formActionLogic.GetFormActionDomainsAsync();
            }

            private List<string> GetAllowIframeOnDomains(object controller)
            {
                if (controller is IRouteBinding)
                {
                    return (controller as IRouteBinding)?.RouteBinding?.AllowIframeOnDomains;
                }
                return null;
            }

            protected override void HeaderXFrameOptions(HttpResponse response)
            {
                if (allowIframeOnDomains != null && allowIframeOnDomains.Count() == 1 && allowIframeOnDomains.Where(d => !d.Contains("*")).Count() == 1)
                {
                    response.SetHeader("X-Frame-Options", $"allow-from https://{allowIframeOnDomains.First()}/");
                }
                else
                {
                    base.HeaderXFrameOptions(response);
                }
            }

            protected override string CspFormAction()
            {
                if (allowFormActionOnDomains == null || allowFormActionOnDomains.Count() < 1)
                {
                    return base.CspFormAction();
                }
                else
                {
                    return $"form-action 'self' {allowFormActionOnDomains.Select(d => $"https://{d}").ToSpaceList()};";
                }
            }

            protected override string CspFrameAncestors()
            {
                if (allowIframeOnDomains == null || allowIframeOnDomains.Count() < 1)
                {
                    return base.CspFrameAncestors();
                }
                else
                {
                    return $"frame-ancestors 'self' {allowIframeOnDomains.Select(d => $"https://{d}").ToSpaceList()};";
                }
            }
        }
    }
}
