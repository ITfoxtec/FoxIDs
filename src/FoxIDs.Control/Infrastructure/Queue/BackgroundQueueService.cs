using FoxIDs.Models.Queue;
using ITfoxtec.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Queue
{
    public class BackgroundQueueService : BackgroundService
    {
        public const string QueueKey = "background_queue_service";
        public const string QueueEventKey = "background_queue_service_event";
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private bool isStopped;

        public BackgroundQueueService(TelemetryLogger logger, IServiceProvider serviceProvider, IConnectionMultiplexer redisConnectionMultiplexer)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                await ReadMessageAndDoWorkAsync(stoppingToken);
                
                var sub = redisConnectionMultiplexer.GetSubscriber();
                var channel = await sub.SubscribeAsync(QueueEventKey);
                channel.OnMessage(async channelMessage =>
                {
                    await ReadMessageAndDoWorkAsync(stoppingToken);
                });
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);

            if (!isStopped)
            {
                var sub = redisConnectionMultiplexer.GetSubscriber();
                await sub.UnsubscribeAsync(QueueKey);
                isStopped = true;
            }
        }

        private async Task ReadMessageAndDoWorkAsync(CancellationToken stoppingToken)
        {
            try
            {
                var db = redisConnectionMultiplexer.GetDatabase();
                var envelope = await db.ListRightPopAsync(QueueKey);
                if (!envelope.IsNull)
                {
                    await DoWorkAsync(envelope, stoppingToken);
                    await ReadMessageAndDoWorkAsync(stoppingToken);
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
                logger.Error(ex, "Unable read message and to do background queue work.");
            }
        }

        public override void Dispose()
        {
            if (!isStopped)
            {
                var sub = redisConnectionMultiplexer.GetSubscriber();
                sub.Unsubscribe(QueueKey);
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
                        scopedLogger.SetScopeProperty(Constants.Logs.TenantName, envelopeObj.TenantName);
                        scopedLogger.SetScopeProperty(Constants.Logs.TrackName, envelopeObj.TrackName);

                        try
                        {
                            scopedLogger.Event($"Start to process '{envelopeObj.Info}'.");
                            scopedLogger.ScopeTrace(() => $"Background queue envelope '{envelope}'", traceType: TraceTypes.Message);
                            var processingService = scope.ServiceProvider.GetRequiredService(envelopeObj.LogicClassType) as IQueueProcessingService;
                            if (processingService == null)
                            {
                                throw new Exception($"Logic type '{envelopeObj.LogicClassType.Name}' is not of type '{nameof(IQueueProcessingService)}'.");
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
    }
}
