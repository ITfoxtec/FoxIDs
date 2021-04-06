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
            private List<string> allowFrameSrcDomains;
            private List<string> allowIframeOnDomains;
            private readonly IServiceProvider serviceProvider;

            public FoxIDsHttpSecurityHeadersActionAttribute(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IWebHostEnvironment env) : base(logger, env)
            {
                this.serviceProvider = serviceProvider;
            }

            protected override void ActionExecutionInit(ActionExecutedContext resultContext)
            {
                base.ActionExecutionInit(resultContext);

                var securityHeaderLogic = serviceProvider.GetService<SecurityHeaderLogic>();
                allowFormActionOnDomains = securityHeaderLogic.GetFormActionDomains();
                allowFrameSrcDomains = securityHeaderLogic.GetFrameSrcDomains();

                allowIframeOnDomains = GetAllowIframeOnDomains(resultContext.Controller as IRouteBinding, securityHeaderLogic);
            }

            private List<string> GetAllowIframeOnDomains(IRouteBinding controller, SecurityHeaderLogic securityHeaderLogic)
            {
                List<string> domains = null;
                if (controller != null)
                {
                    var controllerDomains = controller?.RouteBinding?.AllowIframeOnDomains;
                    if (controllerDomains != null)
                    {
                        domains = controllerDomains;
                    }
                }

                var logicDomains = securityHeaderLogic.GetAllowIframeOnDomains();
                if (logicDomains != null)
                {
                    if (domains == null)
                    {
                        domains = logicDomains;
                    }
                    else
                    {
                        domains.ConcatOnce(logicDomains);
                    }
                }

                return domains;
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
                    return $"form-action 'self' {allowFormActionOnDomains.Select(d => d == "*" ? d : $"https://{d}").ToSpaceList()};";
                }
            }

            protected override string CspFrameSrc()
            {
                if (allowFrameSrcDomains == null || allowFrameSrcDomains.Count() < 1)
                {
                    return base.CspFrameSrc();
                }
                else
                {
                    return $"frame-src {allowFrameSrcDomains.Select(d => d == "*" ? d : $"https://{d}").ToSpaceList()};";
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
