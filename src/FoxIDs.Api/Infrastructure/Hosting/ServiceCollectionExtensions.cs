using AutoMapper;
using FoxIDs.Infrastructure.KeyVault;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using FoxIDs.MappingProfiles;
using FoxIDs.Models.Config;
using ITfoxtec.Identity.Discovery;
using ITfoxtec.Identity.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Reflection;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogic(this IServiceCollection services)
        {
            services.AddSharedLogic();

            services.AddTransient<SecretHashLogic>();

            services.AddTransient<ValidatePartyLogic>();
            services.AddTransient<ValidateSamlPartyLogic>();

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

            services.AddApiSwagger();
            services.AddAutoMapper();

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
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(Constants.Api.Version, new Info { Title = "FoxIDs API", Version = Constants.Api.Version });
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[] { }},
                });

                c.TagActionsBy(s => new[]
                {
                    GetTagActionsBy(s.ActionDescriptor as ControllerActionDescriptor)
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            return services;
        }

        public static IServiceCollection AddAutoMapper(this IServiceCollection services)
        {
            services.AddSingleton(serviceProvider =>
            {
                var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
                var mappingConfig = new MapperConfiguration(mc =>
                {
                    mc.AllowNullCollections = true;

                    mc.AddProfile(new MasterMappingProfile());
                    mc.AddProfile(new TenantMappingProfiles(httpContextAccessor));
                });

                return mappingConfig.CreateMapper();
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
