using FoxIDs.Infrastructure.Hosting;
using FoxIDs.Models.Config;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;

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

            services.AddMvcCore(options =>
            {
                options.EnableEndpointRouting = false;
            })
                .AddAuthorization()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonFormatters()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.Converters.Add(new StringEnumConverter(typeof(CamelCaseNamingStrategy)));
                })
                .AddFoxIDsApiExplorer();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (CurrentEnvironment.IsDevelopment())
            {
                TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
            }
            else
            {
                app.UseHsts();
            }

            app.UseProxyClientIpMiddleware();
            app.UseEnLocalization();
            app.UseApiSwagger();

            app.UseMvc(routes =>
            {
                routes.Routes.Add(new FoxIDsApiRouter(routes.DefaultHandler));
            });
        }
    }
}
