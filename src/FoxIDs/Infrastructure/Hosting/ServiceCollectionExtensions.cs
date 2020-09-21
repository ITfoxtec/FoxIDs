using Azure.Core;
using Azure.Identity;
using FoxIDs.Infrastructure.Localization;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using StackExchange.Redis;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogic(this IServiceCollection services)
        {
            services.AddSharedLogic();

            services.AddSingleton<LocalizationLogic>();            

            services.AddTransient<SequenceLogic>();
            services.AddTransient<FormActionLogic>();
            services.AddTransient<TrackKeyLogic>();

            services.AddTransient<LoginUpLogic>();
            services.AddTransient<LogoutUpLogic>();
            services.AddTransient<SecretHashLogic>();
            services.AddTransient<FailingLoginLogic>();            
            services.AddTransient<UserAccountLogic>();
            services.AddTransient<SessionLogic>();
            services.AddTransient<ClaimTransformationsLogic>();            

            services.AddTransient<OidcDiscoveryLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>();
            services.AddTransient<OidcDiscoveryLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>();

            services.AddTransient<JwtLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim>>();
            services.AddTransient<JwtLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
            services.AddTransient<OAuthAuthCodeGrantLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim>>();
            services.AddTransient<OAuthAuthCodeGrantLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
            services.AddTransient<OAuthRefreshTokenGrantLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim>>();
            services.AddTransient<OAuthRefreshTokenGrantLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
            services.AddTransient<OAuthResourceScopeLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim>>();
            services.AddTransient<OAuthResourceScopeLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();

            services.AddTransient<OAuthTokenDownLogic<OAuthDownParty, OAuthDownClient, OAuthDownScope, OAuthDownClaim>>();

            services.AddTransient<OidcAuthUpLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>();
            services.AddTransient<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>();
            services.AddTransient<OidcTokenDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>();
            services.AddTransient<OidcUserInfoDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>();            
            services.AddTransient<OidcEndSessionDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>();

            services.AddTransient<ClaimsLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim>>();
            services.AddTransient<ClaimsLogic<OidcDownClient, OidcDownScope, OidcDownClaim>>();
            services.AddTransient<Saml2ConfigurationLogic>();
            services.AddTransient<SamlMetadataLogic>();
            services.AddTransient<SamlAuthnUpLogic>();
            services.AddTransient<SamlAuthnDownLogic>();
            services.AddTransient<SamlLogoutUpLogic>();
            services.AddTransient<SamlLogoutDownLogic>();

            return services;
        }

        public static IServiceCollection AddRepository(this IServiceCollection services)
        {
            services.AddSharedRepository();

            services.AddTransient(typeof(SingleCookieRepository<>));

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, FoxIDsSettings settings, IWebHostEnvironment env)
        {
            services.AddSharedInfrastructure();

            services.AddSingleton<IStringLocalizer, FoxIDsStringLocalizer>();
            services.AddSingleton<IStringLocalizerFactory, FoxIDsStringLocalizerFactory>();
            services.AddSingleton<IValidationAttributeAdapterProvider, LocalizedValidationAttributeAdapterProvider>();

            services.AddScoped<FoxIDsRouteTransformer>();
            services.AddScoped<ICorsPolicyProvider, CorsPolicyProvider>();

            services.AddSingleton<OidcDiscoveryHandler>();

            if (!env.IsDevelopment())
            {
                services.AddSingleton<TokenCredential, DefaultAzureCredential>();
            }
            else
            {
                services.AddSingleton<TokenCredential>(serviceProvider =>
                {
                    return new ClientSecretCredential(settings.KeyVault.TenantId, settings.KeyVault.ClientId, settings.KeyVault.ClientSecret);
                });
            }

            services.AddHttpContextAccessor();
            services.AddHttpClient();

            var connectionMultiplexer = ConnectionMultiplexer.Connect(settings.RedisCache.ConnectionString);
            services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

            services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(connectionMultiplexer, "data_protection_keys");

            services.AddStackExchangeRedisCache(options => {
                options.Configuration = settings.RedisCache.ConnectionString;
                options.InstanceName = "cache";
            });

            return services;
        }
    }
}
