using FoxIDs.Infrastructure.KeyVault;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using ITfoxtec.Identity.Discovery;
using ITfoxtec.Identity.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogic(this IServiceCollection services)
        {
            services.AddSharedLogic();

            return services;
        }

        public static IServiceCollection AddRepository(this IServiceCollection services)
        {
            services.AddSharedRepository();

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, Settings settings, IHostingEnvironment env)
        {
            services.AddSharedInfrastructure();

            services.AddSingleton(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                return new OidcDiscoveryHandler(httpClientFactory);
            });

            if (env.IsProduction())
            {
                services.AddSingleton(serviceProvider =>
                {
                    return FoxIDsKeyVaultClient.GetManagedClient();
                });
            }
            else
            {
                services.AddTransient<TokenHelper>();
                services.AddSingleton(serviceProvider =>
                {
                    var tokenHelper = serviceProvider.GetService<TokenHelper>();
                    return FoxIDsKeyVaultClient.GetClient(settings, tokenHelper);
                });
            }

            services.AddHttpContextAccessor();
            services.AddHttpClient();

            return services;
        }

        public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, FoxIDsApiSettings settings)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(JwtBearerMultipleTenantsHandler.AuthenticationScheme)
                .AddScheme<JwtBearerMultipleTenantsOptions, JwtBearerMultipleTenantsHandler>(JwtBearerMultipleTenantsHandler.AuthenticationScheme, options =>
                {
                    options.FoxIDsEndpoint = settings.FoxIDsEndpoint;
                    options.DownParty = settings.DownParty;
                });

            services.AddAuthorization(options =>
            {
                MasterScopeAuthorizeAttribute.AddPolicy(options);
                TenantScopeAuthorizeAttribute.AddPolicy(options);
            });

            return services;
        }
    }
}
