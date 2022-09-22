using FoxIDs.Client.Logic;
using FoxIDs.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tewr.Blazor.FileReader;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Infrastructure.Security;
using Blazored.SessionStorage;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Logging;
using System.IdentityModel.Tokens.Jwt;
using Blazored.Toast;

namespace FoxIDs.Client.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogic(this IServiceCollection services)
        {
            services.AddScoped<RouteBindingLogic>();
            services.AddScoped<ControlClientSettingLogic>();
            services.AddScoped<NotificationLogic>();
            services.AddScoped<TrackSelectedLogic>();
            services.AddScoped<ClipboardLogic>();

            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<ClientService>();
            services.AddScoped<TenantService>();
            services.AddScoped<MyTenantService>();
            services.AddScoped<TrackService>();
            services.AddScoped<DownPartyService>();
            services.AddScoped<UpPartyService>();
            services.AddScoped<UserService>();
            services.AddScoped<HelpersService>();
            
            services.AddScoped<PlanService>();
            services.AddScoped<RiskPasswordService>();

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, WebAssemblyHostConfiguration configuration, IWebAssemblyHostEnvironment hostEnvironment)
        {
            services.AddHttpClient(BaseService.HttpClientSecureLogicalName, client => client.BaseAddress = new Uri(hostEnvironment.BaseAddress))
               .AddHttpMessageHandler<AccessTokenMessageHandler>()
               .AddHttpMessageHandler<CheckResponseMessageHandler>();
            services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(BaseService.HttpClientSecureLogicalName));

            services.AddHttpClient(BaseService.HttpClientLogicalName, client => client.BaseAddress = new Uri(hostEnvironment.BaseAddress))   
                .AddHttpMessageHandler<CheckResponseMessageHandler>();
            services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(BaseService.HttpClientLogicalName));

            var settings = new ClientSettings();
            configuration.Bind("Settings", settings);
            services.AddSingleton(settings);

            services.AddTenantOpenidConnectPkce();
            services.AddTransient<CheckResponseMessageHandler>();

            services.AddFileReaderService(options => options.UseWasmSharedBuffer = false);

            services.AddBlazoredToast();

            return services;
        }

        private static IServiceCollection AddTenantOpenidConnectPkce(this IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;

            services.AddBlazoredSessionStorage();

            services.AddSingleton<OpenidConnectPkceSettings>();
            services.AddScoped<OpenidConnectPkce, TenantOpenidConnectPkce>();
            services.AddSingleton(sp => new OidcDiscoveryHandler(sp.GetService<IHttpClientFactory>()));

            services.AddScoped<AuthenticationStateProvider, OidcAuthenticationStateProvider>();
            services.AddTransient<AccessTokenMessageHandler>();

            services.AddOptions();
            services.AddAuthorizationCore();

            // Added to resolve error: Newtonsoft.Json.JsonSerializationException: Unable to find a default constructor to use for type System.IdentityModel.Tokens.Jwt.JwtPayload. Path 'sub', line 1, position 7.
            // https://github.com/mono/linker/issues/870
            _ = new JwtHeader();
            _ = new JwtPayload();

            return services;
        }
    }
}
