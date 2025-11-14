using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class FoxIDsControlHttpSecurityHeadersAttribute : HttpSecurityHeadersAttribute
    {
        public FoxIDsControlHttpSecurityHeadersAttribute() : base(typeof(FoxIDsControlHttpSecurityHeadersActionAttribute))
        { }

        public class FoxIDsControlHttpSecurityHeadersActionAttribute : HttpSecurityHeadersActionAttribute
        {
            private readonly FoxIDsControlSettings settings;

            public FoxIDsControlHttpSecurityHeadersActionAttribute(FoxIDsControlSettings settings, IWebHostEnvironment environment) : base(environment)
            {
                this.settings = settings;
            }

            public void ApplyFromMiddleware(HttpContext httpContext, bool isHtml)
            {
                isHtmlContent = isHtml;
                SetHeaders(httpContext);
            }

            protected override string CspScriptSrc(HttpContext httpContext)
            {
                // Blazor WebAssembly needs 'wasm-unsafe-eval'
                var scriptSrc = "script-src 'self' 'wasm-unsafe-eval' 'unsafe-eval'";

                if (settings.Payment?.EnablePayment == true)
                {
                    scriptSrc = $"{scriptSrc} {"js.mollie.com".DomainToOrigin(httpContext.Request.Scheme)};";
                }

                return $"{scriptSrc};";
            }
        }
    }
}
