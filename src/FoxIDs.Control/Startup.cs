using FoxIDs.Infrastructure.Hosting;
using FoxIDs.Models.Config;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using System.Text.Json.Serialization;
using FoxIDs.Logic.Seed;

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
            var settings = services.BindConfig<FoxIDsControlSettings>(Configuration, nameof(Settings));
            // Also add as Settings
            services.AddSingleton<Settings>(settings);

            if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                var appInsightsSettings = Configuration.BindConfig<ApplicationInsights>(nameof(ApplicationInsights), validate: false);
                services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { ConnectionString = appInsightsSettings.ConnectionString }));
            }

            services.AddInfrastructure(settings, CurrentEnvironment);
            services.AddRepository(settings);
            services.AddLogic(settings);

            services.AddAuthenticationAndAuthorization(settings);

            services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                })
                .AddFoxIDsApiExplorer();
        }

        public void Configure(IApplicationBuilder app, Settings settings)
        {
            app.UseLoggingMiddleware();

            if (!CurrentEnvironment.IsDevelopment())
            {
                app.UseHsts();
            }

            if (!settings.UseHttp)
            {
                app.UseHttpsRedirection();
            }

            app.UseProxyMiddleware();
            app.UseEnLocalization();
            app.UseApiSwagger();

            app.Map($"/{Constants.Routes.ApiPath}", app =>
            {
                app.UseApiExceptionMiddleware();
                app.UseApiRouteBindingMiddleware();

                app.UseCors(builder =>
                {
                    builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });

                app.UseRouting();
                #pragma warning disable ASP0001 // Authorization middleware is incorrectly configured.
                app.UseAuthorization();
                #pragma warning restore ASP0001 // Authorization middleware is incorrectly configured.
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapDynamicControllerRoute<FoxIDsApiRouteTransformer>($"{{**{Constants.Routes.RouteTransformerPathKey}}}");
                });
            });

            app.UseExceptionHandler($"/{Constants.Routes.DefaultSiteController}/{Constants.Routes.ErrorAction}");
            if (CurrentEnvironment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseClientRouteBindingMiddleware();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDynamicControllerRoute<FoxIDsClientRouteTransformer>($"{{**{Constants.Routes.RouteTransformerPathKey}}}");
            });
        }
    }
}
