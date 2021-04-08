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
                SetHeaders(resultContext.HttpContext.Response);
            }

            protected virtual void ActionExecutionInit(ActionExecutedContext resultContext)
            {
                isHtmlContent = IsHtmlContent(resultContext.Result);
            }

            protected virtual void SetHeaders(HttpResponse response)
            {
                logger.ScopeTrace($"Adding http security headers. Is {(isHtmlContent ? string.Empty : "not")} view.");

                response.SetHeader("X-Content-Type-Options", "nosniff");
                response.SetHeader("Referrer-Policy", "no-referrer");
                response.SetHeader("X-XSS-Protection", "1; mode=block");

                if (isHtmlContent)
                {
                    HeaderXFrameOptions(response);
                }

                var csp = CreateCsp().ToSpaceList();
                if (!csp.IsNullOrEmpty())
                {
                    response.SetHeader("Content-Security-Policy", csp);
                    response.SetHeader("X-Content-Security-Policy", csp);
                }

                logger.ScopeTrace($"Http security headers added.");
            }

            private bool IsHtmlContent(IActionResult result)
            {
                if (result is ViewResult)
                {
                    return true;
                }
                else if (result is ContentResult)
                {
                    if ("text/html".Equals((result as ContentResult).ContentType, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            protected virtual void HeaderXFrameOptions(HttpResponse response)
            {
                response.SetHeader("X-Frame-Options", "deny");
            }

            protected virtual IEnumerable<string> CreateCsp()
            {
                if (isHtmlContent)
                {
                    yield return "block-all-mixed-content;";

                    yield return "default-src 'self';";
                    yield return "connect-src 'self' https://dc.services.visualstudio.com/v2/track;";
                    //yield return "font-src 'self';";
                    yield return "img-src 'self' data: 'unsafe-inline';";
                    yield return "script-src 'self' 'unsafe-inline' https://az416426.vo.msecnd.net;";
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
