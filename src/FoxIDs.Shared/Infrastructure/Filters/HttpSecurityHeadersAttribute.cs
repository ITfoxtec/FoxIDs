using ITfoxtec.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class HttpSecurityHeadersAttribute : TypeFilterAttribute
    {
        public HttpSecurityHeadersAttribute() : base(typeof(HttpSecurityHeadersActionAttribute))
        {
        }

        private class HttpSecurityHeadersActionAttribute : IAsyncActionFilter
        {
            private readonly TelemetryScopedLogger logger;
            private readonly IHostingEnvironment env;

            public HttpSecurityHeadersActionAttribute(TelemetryScopedLogger logger, IHostingEnvironment env)
            {
                this.logger = logger;
                this.env = env;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var resultContext = await next();

                var response = resultContext.HttpContext.Response;
                var result = resultContext.Result;
                var allowIframeOnDomains = (resultContext.Controller as IAllowIframeOnDomains)?.AllowIframeOnDomains;
                SetHeaders(response, result, allowIframeOnDomains);
            }

            public void SetHeaders(HttpResponse response, IActionResult result, List<string> allowIframeOnDomains)
            {
                logger.ScopeTrace($"Adding http security headers. Is {(IsView(result) ? string.Empty : "not")} view.");

                response.SetHeader("X-Content-Type-Options", "nosniff");
                response.SetHeader("Referrer-Policy", "no-referrer");
                response.SetHeader("X-XSS-Protection", "1; mode=block");

                if (IsView(result))
                {
                    HeaderXFrameOptions(response, allowIframeOnDomains);
                }

                var csp = CreateCsp(IsView(result), allowIframeOnDomains).ToSpaceList();
                if (!csp.IsNullOrEmpty())
                {
                    response.SetHeader("Content-Security-Policy", csp);
                    response.SetHeader("X-Content-Security-Policy", csp);
                }

                logger.ScopeTrace($"Http security headers added.");
            }

            private bool IsView(IActionResult result)
            {
                return result is ViewResult;
            }

            private void HeaderXFrameOptions(HttpResponse response, List<string> allowIframeOnDomains)
            {
                if (allowIframeOnDomains != null && allowIframeOnDomains.Count() == 1 && allowIframeOnDomains.Where(d => !d.Contains("*")).Count() == 1)
                {
                    response.SetHeader("X-Frame-Options", $"allow-from https://{allowIframeOnDomains.First()}/");
                }
                else
                {
                    response.SetHeader("X-Frame-Options", "deny");
                }
            }

            private IEnumerable<string> CreateCsp(bool isView, List<string> allowIframeOnDomains)
            {
                if (isView)
                {
                    yield return "block-all-mixed-content;";

                    yield return "default-src 'self';";
                    yield return "connect-src 'self' https://dc.services.visualstudio.com/v2/track;";
                    //yield return "font-src 'self';";
                    yield return "img-src 'self' data: 'unsafe-inline';";
                    yield return "script-src 'self' 'unsafe-inline' https://az416426.vo.msecnd.net;";
                    yield return "style-src 'self' 'unsafe-inline';";

                    yield return "base-uri 'self';";
                    yield return "form-action 'self';";

                    yield return "sandbox allow-forms allow-popups allow-same-origin allow-scripts;";

                    yield return CspFrameAncestors(allowIframeOnDomains);
                }

                if (!env.IsDevelopment())
                {
                    yield return "upgrade-insecure-requests;";
                }
            }

            private string CspFrameAncestors(List<string> allowIframeOnDomains)
            {
                if (allowIframeOnDomains == null || allowIframeOnDomains.Count() < 1)
                {
                    return "frame-ancestors 'none';";
                }
                else
                {
                    return $"frame-ancestors {allowIframeOnDomains.Select(d => $"https://{d}").ToSpaceList()};";
                }
            }

        }
    }
}
