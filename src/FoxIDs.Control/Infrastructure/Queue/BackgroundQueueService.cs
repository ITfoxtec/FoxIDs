using FoxIDs.Models.Queue;
using ITfoxtec.Identity;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using FoxIDs.Models.Config;
using FoxIDs.Logic;

namespace FoxIDs.Infrastructure.Queue
{
    public class BackgroundQueueService : BackgroundService
    {
        public const string QueueEventKey = "background_queue_service_event";
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IQueueProvider queueProvider;
        private bool isStopped;

        public BackgroundQueueService(FoxIDsControlSettings settings, TelemetryLogger logger, IServiceProvider serviceProvider, IQueueProvider queueProvider)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.queueProvider = queueProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!settings.DisableBackgroundQueueService)
            {
                await using var processor = await queueProvider.CreateProcessorAsync(QueueEventKey);
                if (!stoppingToken.IsCancellationRequested)
                {
                    processor.ProcessAsync += DoWorkAsync;
                }

                await Task.Delay(Timeout.Infinite, stoppingToken);

                if (!isStopped)
                {
                    isStopped = true;
                }
            }
            else
            {
                isStopped = true;
            }
        }

        public override void Dispose()
        {
            if (!isStopped)
            {
                isStopped = true;
            }
            base.Dispose();
        }

        public async Task DoWorkAsync(string envelope, CancellationToken stoppingToken)
        {
            try
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    var envelopeObj = envelope.ToObject<QueueEnvelope>();
                    await envelopeObj.ValidateObjectAsync();
                    using (IServiceScope scope = serviceProvider.CreateScope())
                    {
                        var scopedLogger = scope.ServiceProvider.GetRequiredService<TelemetryScopedLogger>();
                        if (envelopeObj.Logging != null)
                        {
                            scopedLogger.Logging = envelopeObj.Logging;
                        }
                        if (!envelopeObj.ApplicationInsightsConnectionString.IsNullOrEmpty())
                        {
                            var telemetryClient = new TelemetryClient(new TelemetryConfiguration { ConnectionString = envelopeObj.ApplicationInsightsConnectionString });
                            scopedLogger.TelemetryLogger = new TelemetryLogger(telemetryClient);
                        }
                        scopedLogger.SetScopeProperty(Constants.Logs.TenantName, envelopeObj.TenantName);
                        scopedLogger.SetScopeProperty(Constants.Logs.TrackName, envelopeObj.TrackName);

                        try
                        {
                            scopedLogger.Event($"Start to process '{envelopeObj.Info}'.");
                            scopedLogger.ScopeTrace(() => $"Background queue envelope '{envelope}'", traceType: TraceTypes.Message);

                            var processingService = scope.ServiceProvider.GetRequiredService(GetTypeFromFullName(envelopeObj.LogicClassTypeFullName)) as IQueueProcessingService;
                            if (processingService == null)
                            {
                                throw new Exception($"Logic type '{envelopeObj.LogicClassTypeFullName}' is not of type '{nameof(IQueueProcessingService)}'.");
                            }

                            await processingService.DoWorkAsync(scopedLogger, envelopeObj.TenantName, envelopeObj.TrackName, envelopeObj.Message, stoppingToken);
                            scopedLogger.Event($"Done processing '{envelopeObj.Info}'.");
                        }
                        catch (Exception ex)
                        {
                            scopedLogger.Error(ex, "Background queue error.");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable do background queue work.");
            }
        }

        private Type GetTypeFromFullName(string logicClassTypeFullName)
        {
            try
            {
                var type = Type.GetType(logicClassTypeFullName);
                if (type == null)
                {
                    throw new Exception($"Type not found.");
                }
                return type;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to find type by full name '{logicClassTypeFullName}'.", ex);
            }
        }
    }
}
