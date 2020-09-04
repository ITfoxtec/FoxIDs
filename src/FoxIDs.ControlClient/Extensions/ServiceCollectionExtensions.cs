using FoxIDs.Client.Infrastructure.Security;
using Blazored.SessionStorage;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using System.Net.Http;

namespace FoxIDs.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTenantOpenidConnectPkce(this IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;

            services.AddBlazoredSessionStorage();

            services.AddSingleton<OpenidConnectPkceSettings>();
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
