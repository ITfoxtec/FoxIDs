using AutoMapper;
using Azure.Core;
using Azure.Identity;
using FoxIDs.Infrastructure.Queue;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using FoxIDs.Logic.Seed;
using FoxIDs.MappingProfiles;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Reflection;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogic(this IServiceCollection services, Settings settings)
        {
            services.AddSharedLogic(settings);

            services.AddSingleton<EmbeddedResourceLogic>();

            services.AddTransient<SeedLogic>();
            services.AddTransient<MasterTenantDocumentsSeedLogic>();
            services.AddTransient<MainTenantDocumentsSeedLogic>();

            services.AddTransient<DownPartyAllowUpPartiesQueueLogic>();

            services.AddTransient<BaseAccountLogic>();
            services.AddTransient<SecretHashLogic>();

            services.AddTransient<MasterTenantLogic>();
            services.AddTransient<TrackLogic>();

            services.AddTransient<LogAnalyticsWorkspaceProvider>();   
            services.AddTransient<UsageLogLogic>();            

            services.AddTransient<ValidateModelGenericPartyLogic>();
            services.AddTransient<ValidateModelOAuthOidcPartyLogic>();

            services.AddTransient<ValidateApiModelGenericPartyLogic>();
            services.AddTransient<ValidateApiModelLoginPartyLogic>();
            services.AddTransient<ValidateApiModelOAuthOidcPartyLogic>();
            services.AddTransient<ValidateApiModelSamlPartyLogic>();
            services.AddTransient<ValidateApiModelTrackLinkPartyLogic>();
            services.AddTransient<ValidateApiModelDynamicElementLogic>();
            services.AddTransient<ValidateApiModelExternalUserLogic>();

            services.AddTransient<OidcDiscoveryReadLogic<OAuthUpParty, OAuthUpClient>>();
            services.AddTransient<OidcDiscoveryReadLogic<OidcUpParty, OidcUpClient>>();
            services.AddTransient<OidcDiscoveryReadUpLogic<OAuthUpParty, OAuthUpClient>>();
            services.AddTransient<OidcDiscoveryReadUpLogic<OidcUpParty, OidcUpClient>>();

            services.AddTransient<SamlMetadataReadLogic>();
            services.AddTransient<SamlMetadataReadUpLogic>();

            return services;
        }

        public static IServiceCollection AddRepository(this IServiceCollection services, Settings settings)
        {
            services.AddSharedRepository(settings);

            switch (settings.Options.DataStorage)
            {
                case DataStorageOptions.File:
                    services.AddHostedService<BackgroundFileDataService>();
                    break;
                case DataStorageOptions.PostgreSql:
                    services.AddHostedService<BackgroundPgDataService>();
                    break;
            }

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, FoxIDsControlSettings settings, IWebHostEnvironment environment)
        {
            services.AddSharedInfrastructure(settings, environment);

            services.AddScoped<FoxIDsApiRouteTransformer>();

            services.AddSingleton<BackgroundQueue>();
            services.AddHostedService<BackgroundQueueService>();

            if (settings.Options.Log == LogOptions.ApplicationInsights || settings.Options.KeyStorage == KeyStorageOptions.KeyVault)
            {
                if (!environment.IsDevelopment())
                {
                    services.AddSingleton<TokenCredential, DefaultAzureCredential>();
                }
                else
                {
                    services.AddSingleton<TokenCredential>(serviceProvider =>
                    {
                        return new ClientSecretCredential(settings.ServerClientCredential?.TenantId, settings.ServerClientCredential?.ClientId, settings.ServerClientCredential?.ClientSecret);
                    });
                }
            }

            if (settings.Options.Cache == CacheOptions.Redis)
            {
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(settings.RedisCache.ConnectionString));
            }

            services.AddApiSwagger();
            services.AddAutoMapper();

            return services;
        }

        public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, FoxIDsControlSettings settings)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(JwtBearerMultipleTenantsHandler.AuthenticationScheme)
                .AddScheme<JwtBearerMultipleTenantsOptions, JwtBearerMultipleTenantsHandler>(JwtBearerMultipleTenantsHandler.AuthenticationScheme, options =>
                {
                    options.FoxIDsEndpoint = settings.FoxIDsBackendEndpoint.IsNullOrWhiteSpace() ? settings.FoxIDsEndpoint : settings.FoxIDsBackendEndpoint;
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
                c.SwaggerDoc(Constants.ControlApi.Version, new OpenApiInfo { Title = "FoxIDs Control API", Version = Constants.ControlApi.Version });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"              
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                         new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}
                    }
                });

                c.TagActionsBy(s =>
                [
                    GetTagActionsBy(s.ActionDescriptor as ControllerActionDescriptor)
                ]);

                c.OrderActionsBy((ad) => 
                {
                    var controllerActionDescriptor = ad.ActionDescriptor as ControllerActionDescriptor;
                    if (controllerActionDescriptor != null)
                    {
                        if(controllerActionDescriptor.ControllerName.Equals(Constants.Routes.HealthController, StringComparison.OrdinalIgnoreCase))
                        {
                            // order health check last
                            return $"z {controllerActionDescriptor.ControllerName}";
                        }
                        return controllerActionDescriptor.ControllerName;
                    }
                    return null; 
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
            if (controllerName.StartsWith(Constants.Routes.ApiControllerPreMasterKey, StringComparison.OrdinalIgnoreCase))
            {
                return $"master {controllerName.Substring(1)}";
            }
            else if (controllerName.StartsWith(Constants.Routes.ApiControllerPreTenantTrackKey, StringComparison.OrdinalIgnoreCase))
            {
                return $"tenant {controllerName.Substring(1)}";
            }
            else if (controllerName.Equals(Constants.Routes.HealthController, StringComparison.OrdinalIgnoreCase))
            {
                return controllerName;
            }
            else
            {
                throw new NotSupportedException("Only master and tenant controller supported.");
            }
        }

    }
}
