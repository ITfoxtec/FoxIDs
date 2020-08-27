﻿using FoxIDs.Client.Logic;
using FoxIDs.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tewr.Blazor.FileReader;

namespace FoxIDs.Client.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        const string httpClientLogicalName = "FoxIDs.ControlAPI";

        public static IServiceCollection AddLogic(this IServiceCollection services)
        {
            services.AddScoped<RouteBindingLogic>();
            services.AddScoped<NotificationLogic>();
            services.AddScoped<TrackSelectedLogic>();

            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<TenantService>();
            services.AddScoped<TrackService>();
            services.AddScoped<DownPartyService>();
            services.AddScoped<UpPartyService>();
            services.AddScoped<UserService>();

            services.AddScoped<RiskPasswordService>();

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, WebAssemblyHostConfiguration configuration, IWebAssemblyHostEnvironment hostEnvironment)
        {
            services.AddHttpClient(httpClientLogicalName, client => client.BaseAddress = new Uri(hostEnvironment.BaseAddress))
               .AddHttpMessageHandler<AccessTokenMessageHandler>()
               .AddHttpMessageHandler<CheckResponseMessageHandler>();

            services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientLogicalName));

            services.AddTenantOpenidConnectPkce(settings =>
            {
                configuration.Bind("IdentitySettings", settings);
            });

            services.AddTransient<CheckResponseMessageHandler>();

            services.AddFileReaderService(options => options.UseWasmSharedBuffer = true);

            return services;
        }
    }
}
