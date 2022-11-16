using ITfoxtec.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class HttpSecurityHeadersAttribute : TypeFilterAttribute
    {
        public HttpSecurityHeadersAttribute() : base(typeof(HttpSecurityHeadersActionAttribute))
        { }
        public HttpSecurityHeadersAttribute(Type type) : base(type)
        { }

        public class HttpSecurityHeadersActionAttribute : IAsyncActionFilter
        {
            protected bool isHtmlContent;
            private readonly TelemetryScopedLogger logger;
            private readonly IWebHostEnvironment env;

            public HttpSecurityHeadersActionAttribute(TelemetryScopedLogger logger, IWebHostEnvironment env)
            {
                this.logger = logger;
                this.env = env;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var resultContext = await next();

                ActionExecutionInit(resultContext);
                SetHeaders(resultContext.HttpContext);
            }

            protected virtual void ActionExecutionInit(ActionExecutedContext resultContext)
            {
                isHtmlContent = resultContext.Result.IsHtmlContent();
            }

            protected virtual void SetHeaders(HttpContext httpContext)
            {
                logger.ScopeTrace(() => $"Adding http security headers. Is {(isHtmlContent ? string.Empty : "not")} view.");

                httpContext.Response.SetHeader("X-Content-Type-Options", "nosniff");
                httpContext.Response.SetHeader("Referrer-Policy", "no-referrer");
                httpContext.Response.SetHeader("X-XSS-Protection", "1; mode=block");

                if (isHtmlContent)
                {
                    HeaderXFrameOptions(httpContext);
                }

                var csp = CreateCsp(httpContext).ToSpaceList();
                if (!csp.IsNullOrEmpty())
                {
                    httpContext.Response.SetHeader("Content-Security-Policy", csp);
                    httpContext.Response.SetHeader("X-Content-Security-Policy", csp);
                }

                logger.ScopeTrace(() => $"Http security headers added.");
            }       

            protected virtual void HeaderXFrameOptions(HttpContext httpContext)
            {
                httpContext.Response.SetHeader("X-Frame-Options", "deny");
            }

            protected virtual IEnumerable<string> CreateCsp(HttpContext httpContext)
            {
                if (isHtmlContent)
                {
                    yield return "block-all-mixed-content;";

                    yield return "default-src 'self';";
                    yield return $"connect-src 'self'{GetConnectSrc(httpContext)};"; 

                    var cspImgSrc = CspImgSrc();
                    if (!cspImgSrc.IsNullOrEmpty())
                    {
                        yield return cspImgSrc;
                    }

                    yield return "script-src 'self' 'unsafe-inline';";
                    yield return "style-src 'self' 'unsafe-inline';";

                    yield return "base-uri 'self';";

                    var cspFormAction = CspFormAction();
                    if (!cspFormAction.IsNullOrEmpty())
                    {
                        yield return cspFormAction;
                    }
                    var cspFrameSrc = CspFrameSrc();
                    if (!cspFrameSrc.IsNullOrEmpty())
                    {
                        yield return cspFrameSrc;
                    }

                    yield return "sandbox allow-forms allow-popups allow-same-origin allow-scripts;";

                    yield return CspFrameAncestors();
                }

                if (!env.IsDevelopment())
                {
                    yield return "upgrade-insecure-requests;";
                }
            }

            private string GetConnectSrc(HttpContext httpContext)
            {
#if DEBUG
                if (env.IsDevelopment())
                {
                    return $" *";
                }
#endif
                return string.Empty;
            }

            protected virtual string CspImgSrc()
            {
                return "img-src 'self' data: 'unsafe-inline';";
            }

            protected virtual string CspFormAction()
            {
                return string.Empty;
            }

            protected virtual string CspFrameSrc()
            {
                return string.Empty;
            }            

            protected virtual string CspFrameAncestors()
            {
                return "frame-ancestors 'none';";
            }
        }
    }
}
