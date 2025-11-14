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
            private readonly IWebHostEnvironment environment;

            public HttpSecurityHeadersActionAttribute(IWebHostEnvironment environment)
            {
                this.environment = environment;
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
                httpContext.Response.SetHeader("X-Content-Type-Options", "nosniff");
                httpContext.Response.SetHeader("Referrer-Policy", "strict-origin");
                httpContext.Response.SetHeader("X-XSS-Protection", "1; mode=block");

                if (isHtmlContent)
                {
                    HeaderXFrameOptions(httpContext);

                    var permissionsPolicy = PermissionsPolicy(httpContext);
                    if (!permissionsPolicy.IsNullOrEmpty())
                    {
                        httpContext.Response.SetHeader("Permissions-Policy", permissionsPolicy);
                    }
                }

                var csp = CreateCsp(httpContext).ToSpaceList();
                if (!csp.IsNullOrEmpty())
                {
                    httpContext.Response.SetHeader("Content-Security-Policy", csp);
                    httpContext.Response.SetHeader("X-Content-Security-Policy", csp);
                }
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

                    var cspImgSrc = CspImgSrc(httpContext);
                    if (!cspImgSrc.IsNullOrEmpty())
                    {
                        yield return cspImgSrc;
                    }

                    var cspScriptSrc = CspScriptSrc(httpContext);
                    if (!cspScriptSrc.IsNullOrEmpty())
                    {
                        yield return cspScriptSrc;
                    }

                    yield return "style-src 'self' 'unsafe-inline';";

                    yield return "base-uri 'self';";

                    var cspFormAction = CspFormAction(httpContext);
                    if (!cspFormAction.IsNullOrEmpty())
                    {
                        yield return cspFormAction;
                    }
                    var cspFrameSrc = CspFrameSrc(httpContext);
                    if (!cspFrameSrc.IsNullOrEmpty())
                    {
                        yield return cspFrameSrc;
                    }

                    yield return "sandbox allow-forms allow-popups allow-same-origin allow-scripts;";

                    yield return CspFrameAncestors(httpContext);
                }

                if (httpContext.Request.IsHttps && !environment.IsDevelopment())
                {
                    yield return "upgrade-insecure-requests;";
                }
            }

            private string GetConnectSrc(HttpContext httpContext)
            {
#if DEBUG
                if (environment.IsDevelopment())
                {
                    return $" *";
                }
#endif
                return string.Empty;
            }

            protected virtual string CspImgSrc(HttpContext httpContext)
            {
                return "img-src 'self' data: 'unsafe-inline';";
            }

            protected virtual string CspScriptSrc(HttpContext httpContext)
            {
                return "script-src 'self' 'unsafe-inline';";
            }

            protected virtual string CspFormAction(HttpContext httpContext)
            {
                return string.Empty;
            }

            protected virtual string CspFrameSrc(HttpContext httpContext)
            {
                return string.Empty;
            }            

            protected virtual string CspFrameAncestors(HttpContext httpContext)
            {
                return "frame-ancestors 'none';";
            }

            protected virtual string PermissionsPolicy(HttpContext httpContext)
            {
                return "interest-cohort=()";
            }
        }
    }
}
