using System;
using FoxIDs.Infrastructure.Hosting;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FoxIDs
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        private IConfiguration Configuration { get; }
        private IWebHostEnvironment CurrentEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(Configuration);

            var settings = services.BindConfig<FoxIDsSettings>(Configuration, nameof(Settings));
            // Also add as Settings
            services.AddSingleton<Settings>(settings);

            services.AddInfrastructure(settings, CurrentEnvironment);
            services.AddRepository();
            services.AddLogic();

            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddControllersWithViews()
                .AddMvcLocalization()
                .AddNewtonsoftJson(); 
        }

        public void Configure(IApplicationBuilder app)
        {
            //IApplicationLifetime lifetime
            //lifetime.ApplicationStarted.Register(() =>
            //{
            //    ...  Logge start event ... ?? https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/339
            //});

            if (CurrentEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler($"/{Constants.Routes.DefaultWebSiteController}/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStatusCodePages(context => throw new Exception($"Page '{context.HttpContext.Request.Path}', status code: {context.HttpContext.Response.StatusCode} {ReasonPhrases.GetReasonPhrase(context.HttpContext.Response.StatusCode)}"));
            app.UseStaticFilesCacheControl(CurrentEnvironment);
            app.UseProxyClientIpMiddleware();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDynamicControllerRoute<FoxIDsRouteTransformer>($"{{**{Constants.Routes.RouteTransformerPathKey}}}");
            });
        }
    }
}
