using FoxIDs.Infrastructure.Hosting;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using System;
using System.Text.Json.Serialization;

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
            services.AddApplicationInsightsTelemetry(options => { options.DeveloperMode = CurrentEnvironment.IsDevelopment(); });
            //services.AddApplicationInsightsTelemetryProcessor<TelemetryScopedProcessor>();

            var settings = services.BindConfig<FoxIDsApiSettings>(Configuration, nameof(Settings));
            // Also add as Settings
            services.AddSingleton<Settings>(settings);

            services.AddInfrastructure(settings, CurrentEnvironment);
            services.AddRepository();
            services.AddLogic();

            services.AddAuthenticationAndAuthorization(settings);

            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddCors();

            services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                })
                .AddFoxIDsApiExplorer();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (CurrentEnvironment.IsDevelopment())
            { 
            }
            else
            {
                app.UseHsts();
            }

            app.UseProxyClientIpMiddleware();
            app.UseEnLocalization();
            app.UseApiSwagger();

            app.Map("/api", app =>
            {
                app.UseApiRouteBindingMiddleware();

                app.UseCors(builder =>
                {
                    builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization);
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
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
