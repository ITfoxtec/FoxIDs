using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.ApplicationInsights;
using StackExchange.Redis;

namespace FoxIDs.Logic
{
    public class HealthCheckLogic
    {
        private readonly Settings settings;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly OpenSearchTelemetryLogger openSearchTelemetryLogger;
        private readonly TelemetryClient telemetryClient;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;

        public HealthCheckLogic(
            Settings settings,
            ITenantDataRepository tenantDataRepository,
            OpenSearchTelemetryLogger openSearchTelemetryLogger = null,
            TelemetryClient telemetryClient = null,
            IConnectionMultiplexer redisConnectionMultiplexer = null)
        {
            this.settings = settings;
            this.tenantDataRepository = tenantDataRepository;
            this.openSearchTelemetryLogger = openSearchTelemetryLogger;
            this.telemetryClient = telemetryClient;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
        }

        public bool CanCheckDatabase => tenantDataRepository != null;

        public bool CanCheckLog =>
            settings?.Options?.Log == LogOptions.OpenSearchAndStdoutErrors ||
            settings?.Options?.Log == LogOptions.ApplicationInsights;

        public bool CanCheckCache => settings?.Options?.Cache == CacheOptions.Redis;

        public async Task<HealthCheckResult> CheckDatabaseAsync(CancellationToken cancellationToken)
        {
            if (!CanCheckDatabase)
            {
                return HealthCheckResult.Skipped("database", "Tenant data repository is not available.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var masterTenantId = await Tenant.IdFormatAsync(new Tenant.IdKey { TenantName = Constants.Routes.MasterTenantName });
                var exists = await tenantDataRepository.ExistsAsync<Tenant>(masterTenantId);

                return exists
                    ? HealthCheckResult.Healthy("database", "Tenant documents found.")
                    : HealthCheckResult.Unhealthy("database", "Tenant documents is missing.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("database", ex.Message);
            }
        }

        public async Task<HealthCheckResult> CheckLogAsync(CancellationToken cancellationToken)
        {
            if (!CanCheckLog)
            {
                return HealthCheckResult.Skipped("log", "Log provider is not configured.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors)
                {
                    await openSearchTelemetryLogger.RolloverAliasReadyCheck(cancellationToken);
                    return HealthCheckResult.Healthy("log", "Log alias responds successfully.");
                }

                if (settings.Options.Log == LogOptions.ApplicationInsights)
                {
                    if (telemetryClient == null)
                    {
                        return HealthCheckResult.Skipped("log", "Application Insights telemetry client is not available.");
                    }

                    telemetryClient.TrackTrace(
                        "FoxIDs health check telemetry verification",
                        new Dictionary<string, string>
                        {
                            ["component"] = "health-check",
                            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
                        });

                    telemetryClient.Flush();

                    return HealthCheckResult.Healthy("log", "Accept trace telemetry.");
                }

                return HealthCheckResult.Skipped("log", $"Log option '{settings.Options.Log}' is not supported for health checks.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("log", ex.Message);
            }
        }

        public async Task<HealthCheckResult> CheckCacheAsync(CancellationToken cancellationToken)
        {
            if (!CanCheckCache)
            {
                return HealthCheckResult.Skipped("cache", "Cache is not configured.");
            }

            if (redisConnectionMultiplexer == null)
            {
                return HealthCheckResult.Skipped("cache", "Redis connection multiplexer is not available.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var database = redisConnectionMultiplexer.GetDatabase();
                var response = await database.ExecuteAsync("PING");
                var responseText = response.ToString();

                if (string.Equals(responseText, "PONG", StringComparison.OrdinalIgnoreCase))
                {
                    return HealthCheckResult.Healthy("cache", "Cache returned PONG to PING command.");
                }

                return HealthCheckResult.Unhealthy("cache", $"Cache PING returned unexpected response '{responseText}'.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("cache", ex.Message);
            }
        }
    }
}
