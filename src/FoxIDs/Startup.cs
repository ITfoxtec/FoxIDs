using FoxIDs.Infrastructure.Hosting;
using FoxIDs.Models.Config;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenSearch.Client;

namespace FoxIDs
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            CurrentEnvironment = environment;
        }

        private IConfiguration Configuration { get; }
        private IWebHostEnvironment CurrentEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var settings = services.BindConfig<FoxIDsSettings>(Configuration, nameof(Settings));
            // Also add as Settings
            services.AddSingleton<Settings>(settings);

            if(settings.Options.Log == LogOptions.ApplicationInsights)
            {
                var appInsightsSettings = Configuration.BindConfig<ApplicationInsights>(nameof(ApplicationInsights), validate: false);
                services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { ConnectionString = appInsightsSettings.ConnectionString }));
            }

            services.AddInfrastructure(settings, CurrentEnvironment);
            services.AddRepository(settings);
            services.AddLogic(settings);

            services.AddControllersWithViews()
                .AddMvcLocalization()
                .AddNewtonsoftJson(options => { options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore; }); 
        }

        public void Configure(IApplicationBuilder app, Settings settings)
        {
            app.UseErrorLoggingMiddleware();
            app.UseExceptionHandler($"/{Constants.Routes.ErrorController}/{Constants.Routes.DefaultAction}");

            if (!CurrentEnvironment.IsDevelopment())
            {
                app.UseHsts();
            }

            if (!settings.UseHttp)
            {
                app.UseHttpsRedirection();
            }

            app.UseStaticFilesCacheControl(CurrentEnvironment);
            app.UseProxyMiddleware();

            app.UseRouteBindingMiddleware();
            app.UseCors();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDynamicControllerRoute<FoxIDsRouteTransformer>($"{{**{Constants.Routes.RouteTransformerPathKey}}}");
            });
        }
    }
}
