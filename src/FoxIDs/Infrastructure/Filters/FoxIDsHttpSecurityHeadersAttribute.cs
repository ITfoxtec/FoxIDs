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
            private List<string> allowImgSrcDomains;
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
                allowImgSrcDomains = securityHeaderLogic.GetImgSrcDomains();
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

            protected override void HeaderXFrameOptions(HttpContext httpContext)
            {
                if (allowIframeOnDomains != null && allowIframeOnDomains.Count() == 1 && allowIframeOnDomains.Where(d => !d.Contains("*")).Count() == 1)
                {
                    httpContext.Response.SetHeader("X-Frame-Options", $"allow-from https://{allowIframeOnDomains.First()}/");
                }
                else
                {
                    base.HeaderXFrameOptions(httpContext);
                }
            }

            protected override string CspImgSrc(HttpContext httpContext)
            {
                if (allowImgSrcDomains == null || allowImgSrcDomains.Count() < 1)
                {
                    return base.CspImgSrc(httpContext);
                }
                else
                {
                    return $"img-src 'self' data: 'unsafe-inline' {allowImgSrcDomains.Select(d => d.DomainToOrigin(httpContext.Request.Scheme)).ToSpaceList()};";
                }
            }

            protected override string CspFormAction(HttpContext httpContext)
            {
                if (allowFormActionOnDomains == null || allowFormActionOnDomains.Count() < 1)
                {
                    return base.CspFormAction(httpContext);
                }
                else
                {
                    var formActionOnDomains = allowFormActionOnDomains.Where(d => d == "*").Any() ? "*" : allowFormActionOnDomains.Select(d => d.DomainToOrigin(httpContext.Request.Scheme)).ToSpaceList();
                    return $"form-action 'self' {formActionOnDomains};";
                }
            }

            protected override string CspFrameSrc(HttpContext httpContext)
            {
                if (allowFrameSrcDomains == null || allowFrameSrcDomains.Count() < 1)
                {
                    return base.CspFrameSrc(httpContext);
                }
                else
                {
                    var frameSrcDomains = allowFrameSrcDomains.Where(d => d == "*").Any() ? "*" : allowFrameSrcDomains.Select(d => d.DomainToOrigin(httpContext.Request.Scheme)).ToSpaceList();
                    return $"frame-src {frameSrcDomains};";
                }
            }

            protected override string CspFrameAncestors(HttpContext httpContext)
            {
                if (allowIframeOnDomains == null || allowIframeOnDomains.Count() < 1)
                {
                    return base.CspFrameAncestors(httpContext);
                }
                else
                {
                    return $"frame-ancestors 'self' {allowIframeOnDomains.Select(d => d.DomainToOrigin(httpContext.Request.Scheme)).ToSpaceList()};";
                }
            }
        }
    }
}
