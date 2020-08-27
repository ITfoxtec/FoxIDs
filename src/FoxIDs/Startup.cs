﻿using System;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Hosting;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddApplicationInsightsTelemetry(options => { options.DeveloperMode = CurrentEnvironment.IsDevelopment(); });
            services.AddApplicationInsightsTelemetryProcessor<TelemetryScopedProcessor>();

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

            services.AddCors();

            services.AddControllersWithViews()
                .AddMvcLocalization()
                .AddNewtonsoftJson(options => { options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore; }); 
        }

        public void Configure(IApplicationBuilder app)
        {
            if (CurrentEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler($"/{Constants.Routes.DefaultSiteController}/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            //app.UseStatusCodePages(context => throw new Exception($"Page '{context.HttpContext.Request.Path}', status code: {context.HttpContext.Response.StatusCode} {ReasonPhrases.GetReasonPhrase(context.HttpContext.Response.StatusCode)}"));
            app.UseStaticFilesCacheControl(CurrentEnvironment);
            app.UseProxyClientIpMiddleware();

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
