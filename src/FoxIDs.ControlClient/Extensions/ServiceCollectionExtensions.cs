using FoxIDs.Infrastructure.Security;
using Blazored.SessionStorage;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using System;
using System.Net.Http;

namespace FoxIDs
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTenantOpenidConnectPkce(this IServiceCollection services, Action<OpenidConnectPkceSettings> settings)
        {
            IdentityModelEventSource.ShowPII = true;

            services.AddBlazoredSessionStorage();

            var openIDClientPkceSettings = new OpenidConnectPkceSettings();
            settings(openIDClientPkceSettings);
            services.AddSingleton(openIDClientPkceSettings);

            services.AddScoped<OpenidConnectPkce, TenantOpenidConnectPkce>();
            services.AddSingleton(sp => new OidcDiscoveryHandler(sp.GetService<HttpClient>()));

            services.AddScoped<AuthenticationStateProvider, OidcAuthenticationStateProvider>();
            services.AddTransient<AccessTokenMessageHandler>();

            services.AddOptions();
            services.AddAuthorizationCore();

            return services;
        }
    }
}
