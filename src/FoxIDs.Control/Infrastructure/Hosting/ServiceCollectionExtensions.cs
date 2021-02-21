using AutoMapper;
using Azure.Core;
using Azure.Identity;
using FoxIDs.Infrastructure.KeyVault;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using FoxIDs.Logic.Seed;
using FoxIDs.MappingProfiles;
using FoxIDs.Models.Config;
using ITfoxtec.Identity.Discovery;
using ITfoxtec.Identity.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
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
            
            services.AddTransient<SeedLogic>();
            services.AddTransient<ResourceSeedLogic>();
            services.AddTransient<MasterTenantDocumentsSeedLogic>();

            services.AddTransient<BaseAccountLogic>();
            services.AddTransient<SecretHashLogic>();

            services.AddTransient<MasterTenantLogic>();
            services.AddTransient<TrackLogic>();
            services.AddTransient<OidcDiscoveryReadLogic>();
            services.AddTransient<OidcDiscoveryReadUpLogic>();

            services.AddTransient<ValidateGenericPartyLogic>();
            services.AddTransient<ValidateOAuthOidcPartyLogic>();
            services.AddTransient<ValidateSamlPartyLogic>();

            
            return services;
        }

        public static IServiceCollection AddRepository(this IServiceCollection services)
        {
            services.AddSharedRepository();

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, Settings settings, IWebHostEnvironment env)
        {
            services.AddSharedInfrastructure();

            services.AddScoped<FoxIDsApiRouteTransformer>();

            services.AddSingleton<OidcDiscoveryHandlerService>();
            services.AddHostedService<OidcDiscoveryBackgroundService>();

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
                //TODO Work around. Remove when JsonStringEnumConverter is supported.
                c.DescribeAllEnumsAsStrings();
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
