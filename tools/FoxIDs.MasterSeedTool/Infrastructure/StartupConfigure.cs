﻿using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.MasterSeedTool.Logic;
using FoxIDs.MasterSeedTool.Models;
using FoxIDs.MasterSeedTool.SeedLogic;
using System;
using System.Net.Http;
using System.IO;
using ITfoxtec.Identity.Util;
using ITfoxtec.Identity.Helpers;

namespace FoxIDs.MasterSeedTool.Infrastructure
{
    public class StartupConfigure
    {
        private ServiceCollection services;

        public IServiceProvider ConfigureServices()
        {
            services = new ServiceCollection();

            AddConfiguration();
            AddInfrastructure(services);
            AddLogic(services);
            AddSeedLogic(services);

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        private static void AddSeedLogic(ServiceCollection services)
        {
            services.AddTransient<RiskPasswordSeedLogic>();
        }

        private static void AddLogic(ServiceCollection services)
        {
            services.AddSingleton<AccessLogic>();            
        }

        private static void AddInfrastructure(ServiceCollection services)
        {
            services.AddHttpClient();

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
            services.BindConfig<SeedSettings>(configuration, nameof(SeedSettings));
        }
    }
}
