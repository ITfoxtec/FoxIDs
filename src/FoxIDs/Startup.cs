using System;
using FoxIDs.Infrastructure.Hosting;
using FoxIDs.Models.Config;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoxIDs
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        private IConfiguration Configuration { get; }
        private IHostingEnvironment CurrentEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
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

            services.AddMvc(options => options.EnableEndpointRouting = false)
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddMvcLocalization();
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
                TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler($"/{Constants.Routes.DefaultWebSiteController}/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFilesCacheControl(CurrentEnvironment);
            app.UseProxyClientIpMiddleware();

            app.UseMvc(routes =>
            {                
                routes.Routes.Add(new FoxIDsRouter(routes.DefaultHandler));

                routes.MapRoute(
                    name: "default",
                    template: $"{{controller={Constants.Routes.DefaultWebSiteController}}}/{{action={Constants.Routes.DefaultWebSiteAction}}}/{{id?}}");
            });
        }
    }
}
