using FoxIDs.Infrastructure.KeyVault;
using FoxIDs.Infrastructure.Localization;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity.Discovery;
using ITfoxtec.Identity.Helpers;
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
            services.AddTransient<AccountLogic>();
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

            services.AddSingleton<OidcDiscoveryHandler>();

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

            services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(settings.RedisCache.ConnectionString), "data_protection_keys");

            services.AddStackExchangeRedisCache(options => {
                options.Configuration = settings.RedisCache.ConnectionString;
                options.InstanceName = "cache";
            });

            return services;
        }
    }
}
