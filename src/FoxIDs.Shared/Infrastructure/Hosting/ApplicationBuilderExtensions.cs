using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using System;
using System.Globalization;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseStaticFilesCacheControl(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseStaticFiles();
            }
            else
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = (context) =>
                    {
                        context.Context.Response.SetHeader("X-Content-Type-Options", "nosniff");
                        context.Context.Response.SetHeader("Referrer-Policy", "no-referrer");
                        context.Context.Response.SetHeader("X-XSS-Protection", "1; mode=block");

                        var headers = context.Context.Response.GetTypedHeaders();
                        headers.CacheControl = new CacheControlHeaderValue()
                        {
                            MaxAge = TimeSpan.FromDays(365),
                        };
                    }
                });
            }

            return app;
        }

        public static void UseEnLocalization(this IApplicationBuilder builder)
        {
            var defaultCulture = new CultureInfo("en");
            var requestLocalizationOptions = new RequestLocalizationOptions
            {
                RequestCultureProviders = new IRequestCultureProvider[] { new AcceptLanguageHeaderRequestCultureProvider() },
                SupportedCultures = new[] { defaultCulture },
                SupportedUICultures = new[] { defaultCulture },
                DefaultRequestCulture = new RequestCulture(defaultCulture),

            };
            builder.UseRequestLocalization(requestLocalizationOptions);
        }
    }
}
