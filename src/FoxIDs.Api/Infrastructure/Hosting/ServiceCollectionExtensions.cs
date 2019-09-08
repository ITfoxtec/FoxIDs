using FoxIDs.Infrastructure.KeyVault;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models.Config;
using ITfoxtec.Identity.Discovery;
using ITfoxtec.Identity.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
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

        public static IServiceCollection AddApiSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc(Constants.Api.Version, new Info { Title = "FoxIDs API", Version = Constants.Api.Version });
                s.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
                //s.AddSecurityDefinition("oauth2", new OAuth2Scheme
                //{
                //    Type = "oauth2",
                //    Description = "OAuth2 App Authentication grant",
                //    Flow = "application",
                //    AuthorizationUrl = "https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/authorize",
                //    Scopes = new Dictionary<string, string>
                //    {
                        
                //    }
                //   ,
                //    TokenUrl = "https://login.microsoftonline.com/{TenantId}/oauth2/token"

                //});
                //s.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                //{
                //    {"oauth2",new string[]{ }},
                //});
                s.TagActionsBy(t => new[]
                {
                    GetTagActionsBy(t.ActionDescriptor as ControllerActionDescriptor)
                });

                //c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                //{
                //    { "Bearer", new string[] { } }
                //});
            });

            return services;
        }

        private static string GetTagActionsBy(ControllerActionDescriptor controllerActionDescriptor)
        {
            var controllerName = controllerActionDescriptor.ControllerName.ToLower();
            if (controllerName.StartsWith(Constants.Routes.ApiControllerPreMasterKey))
            {
                return $"master {controllerName.Substring(1)}";
            }
            else if (controllerName.StartsWith(Constants.Routes.ApiControllerPreTenantTrackKey))
            {
                return $"tenant {controllerName.Substring(1)}";
            }
            else
            {
                throw new NotSupportedException("Only master and tenant controller supported.");
            }
        }

    }
}
