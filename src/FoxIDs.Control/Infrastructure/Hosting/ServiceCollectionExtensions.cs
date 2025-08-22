using AutoMapper;
using Azure.Core;
using Azure.Identity;
using FoxIDs.Infrastructure.ApiDescription;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using FoxIDs.Logic.Logs;
using FoxIDs.Logic.Queues;
using FoxIDs.Logic.Seed;
using FoxIDs.Logic.Usage;
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
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Mollie.Api;
using Mollie.Api.Framework;
using OpenSearch.Client;
using OpenSearch.Net;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FoxIDs.Infrastructure.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogic(this IServiceCollection services, FoxIDsControlSettings settings)
        {
            services.AddSharedLogic(settings);

            services.AddTransient<SendEventEmailLogic>();

            services.AddTransient<SeedLogic>();
            services.AddHostedService<SeedBackgroundService>();

            services.AddTransient<MasterTenantDocumentsSeedLogic>();
            services.AddTransient<MainTenantDocumentsSeedLogic>();

            services.AddSingleton<BackgroundQueue>();
            services.AddHostedService<QueueBackgroundService>();
            services.AddTransient<DownPartyAllowUpPartiesQueueLogic>();

            services.AddTransient<BaseAccountLogic>();
            services.AddTransient<SecretHashLogic>();

            services.AddTransient<MasterTenantLogic>();
            services.AddTransient<TrackLogic>();

            services.AddSingleton<LogLogic>();
            services.AddSingleton<UsageLogLogic>();
            if (settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
            {
                services.AddSingleton<LogOpenSearchLogic>();
                services.AddSingleton<UsageLogOpenSearchLogic>();
            }
            else if (settings.Options.Log == LogOptions.ApplicationInsights)
            {
                services.AddSingleton<LogAnalyticsWorkspaceProvider>();
                services.AddSingleton<LogApplicationInsightsLogic>();
                services.AddSingleton<UsageLogApplicationInsightsLogic>();
            }

            services.AddTransient<PartyLogic>();

            services.AddTransient<ValidateModelGenericPartyLogic>();
            services.AddTransient<ValidateModelOAuthOidcPartyLogic>();

            services.AddTransient<ValidateApiModelGenericPartyLogic>();
            services.AddTransient<ValidateApiModelLoginPartyLogic>();
            services.AddTransient<ValidateApiModelOAuthOidcPartyLogic>();
            services.AddTransient<ValidateApiModelSamlPartyLogic>();
            services.AddTransient<ValidateApiModelTrackLinkPartyLogic>();
            services.AddTransient<ValidateApiModelExternalLoginPartyLogic>();
            services.AddTransient<ValidateApiModelDynamicElementLogic>();
            services.AddTransient<ValidateApiModelExternalUserLogic>();

            services.AddTransient<OAuthRefreshTokenGrantDownBaseLogic>();

            services.AddTransient<OidcDiscoveryReadLogic>();
            services.AddTransient<OidcDiscoveryReadModelLogic<OAuthUpParty, OAuthUpClient>>();
            services.AddTransient<OidcDiscoveryReadModelLogic<OidcUpParty, OidcUpClient>>();
            services.AddTransient<OidcDiscoveryReadUpLogic<OAuthUpParty, OAuthUpClient>>();
            services.AddTransient<OidcDiscoveryReadUpLogic<OidcUpParty, OidcUpClient>>();

            services.AddTransient<SamlMetadataReadLogic>();
            services.AddTransient<SamlMetadataReadUpLogic>();

            if (settings.Payment?.EnablePayment == true && settings.Usage?.EnableInvoice == true)
            {
                services.AddHostedService<UsageBackgroundService>();
                services.AddTransient<UsageBackgroundWorkLogic>();
                services.AddTransient<UsageCalculatorLogic>();
                services.AddTransient<UsageInvoicingLogic>();
                services.AddTransient<UsageMolliePaymentLogic>();
            }

            return services;
        }

        public static IServiceCollection AddRepository(this IServiceCollection services, FoxIDsControlSettings settings)
        {
            services.AddSharedRepository(settings);

            if (settings.Options.DataStorage == DataStorageOptions.File)
            {
                services.AddHostedService<BackgroundFileDataService>();
            }
            else if(settings.Options.DataStorage == DataStorageOptions.PostgreSql || settings.Options.Cache == CacheOptions.PostgreSql)
            {
                services.AddHostedService<BackgroundPgDataService>();
            }

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, FoxIDsControlSettings settings, IWebHostEnvironment environment)
        {
            services.AddSharedInfrastructure(settings, environment);

            services.AddScoped<FoxIDsApiRouteTransformer>();
            services.AddScoped<FoxIDsClientRouteTransformer>();

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

            if (settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
            {
                var openSearchNodes = settings.OpenSearchQuery != null ? settings.OpenSearchQuery.Nodes : settings.OpenSearch.Nodes;
                var openSearchQueryLogSettings = new ConnectionSettings(openSearchNodes.Count == 1 ? new SingleNodeConnectionPool(openSearchNodes.First()) : new StaticConnectionPool(openSearchNodes))
                    .RequestTimeout(TimeSpan.FromSeconds(20))
                    .MaxRetryTimeout(TimeSpan.FromSeconds(30))
                    .ThrowExceptions();

                if (settings.OpenSearch.AllowInsecureCertificates)
                {
                    openSearchQueryLogSettings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);
                }

                services.AddSingleton(new OpenSearchClientQueryLog(openSearchQueryLogSettings));
            }

            services.AddApiSwagger(settings);
            services.AddAutoMapper();

            if(settings.Payment?.EnablePayment == true && settings.Usage?.EnableInvoice == true)
            {
                services.AddMollieApi(options => {
                    options.ApiKey = settings.Payment.MollieApiKey;
                    options.RetryPolicy = MollieHttpRetryPolicies.TransientHttpErrorRetryPolicy();
                });
            }

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

        public static IServiceCollection AddApiSwagger(this IServiceCollection services, FoxIDsControlSettings settings)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(Constants.ControlApi.Version, new OpenApiInfo { Title = "FoxIDs Control API", Version = Constants.ControlApi.Version });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header. \r\n\r\n Enter the access token in the Value input below.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
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

                c.SchemaFilter<NullableEnumSchemaFilter>();

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
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();                 
                var mappingConfig = new MapperConfiguration(mc =>
                {
                    mc.AllowNullCollections = true;

                    mc.AddProfile(new MasterMappingProfile());
                    mc.AddProfile(new TenantMappingProfiles(httpContextAccessor));
                    mc.AddProfile(new ExternalMappingProfile());
                }, loggerFactory);

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
