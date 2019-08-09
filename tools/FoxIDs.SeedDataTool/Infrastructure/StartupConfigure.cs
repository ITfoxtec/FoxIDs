using FoxIDs.Logic;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FoxIDs.SeedDataTool.Logic;
using FoxIDs.SeedDataTool.Model;
using FoxIDs.SeedDataTool.SeedLogic;
using System;
using System.Net.Http;
using System.IO;
using UrlCombineLib;
using ITfoxtec.Identity.Helpers;
using FoxIDs.SeedDataTool.Repository;
using FoxIDs.Models.Config;

namespace FoxIDs.SeedDataTool.Infrastructure
{
    public class StartupConfigure
    {
        private ServiceCollection services;

        public IServiceProvider ConfigureServices()
        {
            services = new ServiceCollection();
            services.AddLogging(opt =>
            {
                opt.AddConsole();
                opt.AddDebug();
            });

            AddConfiguration();
            AddInfrastructure(services);
            AddRepository(services);
            AddLogic(services);
            AddSeedLogic(services);

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        private static void AddSeedLogic(ServiceCollection services)
        {
            services.AddTransient<MasterTenantDocumentsSeedLogic>();
            services.AddTransient<PasswordRiskListSeedLogic>();
            services.AddTransient<ResourceSeedLogic>();
        }

        private static void AddLogic(ServiceCollection services)
        {
            services.AddTransient<SecretHashLogic>();

            services.AddTransient<AccessLogic>();            
        }

        private static void AddRepository(ServiceCollection services)
        {
            services.AddSingleton<SimpleTenantRepository>();
        }

        private static void AddInfrastructure(ServiceCollection services)
        {
            services.AddHttpClient();

            services.AddTransient<IHttpContextAccessor, HttpContextAccessorSeedHelper>();

            services.AddTransient<TokenHelper>();
            services.AddSingleton(serviceProvider =>
            {
                var settings = serviceProvider.GetService<SeedSettings>();
                var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

                return new OidcDiscoveryHandler(httpClientFactory, UrlCombine.Combine(settings.Authority, IdentityConstants.OidcDiscovery.Path));
            });
        }

        private void AddConfiguration()
        {
            var builder = new ConfigurationBuilder()
                                 .SetBasePath(Directory.GetCurrentDirectory())
                                 .AddJsonFile("appsettings.json")
                                 .AddJsonFile("appsettings.Development.json", optional: true);

            var configuration = builder.Build();

            var seedSettings = new SeedSettings();
            configuration.Bind(nameof(SeedSettings), seedSettings);
            services.AddSingleton(seedSettings);
            var settings = new Settings();
            configuration.Bind(nameof(Settings), settings);
            services.AddSingleton(settings);
        }
    }
}
