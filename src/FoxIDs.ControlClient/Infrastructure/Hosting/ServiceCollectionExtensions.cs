using FoxIDs.Client.Logic;
using FoxIDs.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Infrastructure.Security;
using Blazored.SessionStorage;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Logging;
using System.IdentityModel.Tokens.Jwt;
using Blazored.Toast;
using ITfoxtec.Identity.Helpers;

namespace FoxIDs.Client.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogic(this IServiceCollection services)
        {
            services.AddSingleton<RouteBindingLogic>();
            services.AddSingleton<ControlClientSettingLogic>();
            services.AddSingleton<UserProfileLogic>();
            services.AddSingleton<NotificationLogic>();
            services.AddSingleton<TrackSelectedLogic>();
            services.AddSingleton<MetadataLogic>();
            services.AddSingleton<ClipboardLogic>();
            services.AddSingleton<ServerErrorLogic>();

            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton<ClientService>();
            services.AddSingleton<TenantService>();
            services.AddSingleton<MyTenantService>();
            services.AddSingleton<TrackService>();
            services.AddSingleton<DownPartyService>();
            services.AddSingleton<UpPartyService>();
            services.AddSingleton<UserService>();
            services.AddSingleton<ExternalUserService>();
            services.AddSingleton<HelpersService>();
            services.AddSingleton<HelpersNoAccessTokenService>();
            
            services.AddSingleton<PlanService>();
            services.AddSingleton<SmsPriceService>();
            services.AddSingleton<RiskPasswordService>();

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, WebAssemblyHostConfiguration configuration, IWebAssemblyHostEnvironment hostEnvironment)
        {
            services.AddHttpClient(BaseService.HttpClientSecureLogicalName, client => client.BaseAddress = new Uri(hostEnvironment.BaseAddress))
               .AddHttpMessageHandler<AccessTokenMessageHandler>()
               .AddHttpMessageHandler<CheckResponseMessageHandler>();

            services.AddHttpClient(BaseService.HttpClientLogicalName, client => client.BaseAddress = new Uri(hostEnvironment.BaseAddress))   
                .AddHttpMessageHandler<CheckResponseMessageHandler>();

            var settings = new ClientSettings();
            configuration.Bind("Settings", settings);
            services.AddSingleton(settings);

            services.AddTenantOpenidConnectPkce();
            services.AddTransient<CheckResponseMessageHandler>();

            services.AddBlazoredToast();

            return services;
        }

        private static IServiceCollection AddTenantOpenidConnectPkce(this IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;

            services.AddBlazoredSessionStorageAsSingleton();

            services.AddSingleton<OpenidConnectPkceSettings>(sp => new OpenidConnectPkceSettings { SessionValidationIntervalSeconds = 5 });
            services.AddSingleton<OpenidConnectPkce, TenantOpenidConnectPkce>();
            services.AddSingleton(sp => new OidcDiscoveryHandler(sp.GetService<IHttpClientFactory>()));
            services.AddSingleton(sp => new OidcHelper(sp.GetService<IHttpClientFactory>(), sp.GetService<OidcDiscoveryHandler>()));

            services.AddSingleton<AuthenticationStateProvider, OidcAuthenticationStateProvider>();
            services.AddSingleton<AccessTokenMessageHandler>();

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
