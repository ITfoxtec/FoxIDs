using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Usage
{
    public class UsageCalculatorLogic : LogicBase
    {
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;

        public UsageCalculatorLogic(TelemetryLogger logger, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task<bool> ShouldStartAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {




                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred during should start calculation check.");
            }

            return false;
        }

        public async Task DoCalculationAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {




                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred during usage calculation.");
            }

        }

        public async Task DoTenantCalculationAsync(string tenantName, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (IServiceScope scope = serviceProvider.CreateScope())
                {
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<TelemetryScopedLogger>();
                    try
                    {
                        scopedLogger.Event($"Start tenant '{tenantName}' usage calculation.", properties: new Dictionary<string, string> { { Constants.Logs.TenantName, tenantName } });






                        scopedLogger.Event("Done calculating usage.");
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.Error(ex, $"Error occurred during tenant '{tenantName}' usage calculation.");
                    }
                }
            }
        }
    }
}
