using FoxIDs.Infrastructure;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Usage
{
    public class UsageBackgroundService : BackgroundService
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ICacheProvider cacheProvider;

        public UsageBackgroundService(FoxIDsControlSettings settings, TelemetryLogger logger, IServiceProvider serviceProvider, ICacheProvider cacheProvider)
        {
            this.settings = settings;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.cacheProvider = cacheProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (settings.Payment.EnablePayment == true && settings.Usage?.EnableInvoice == true)
                {
                    do
                    {
                        var tasksDone = await DoWorkAsync(stoppingToken);
                        if (tasksDone)
                        {
                            var now = DateTime.Now;
                            var endOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(1).AddSeconds(-1);
                            var timeSpanToEndOfMonth = endOfMonth - now;
                            var waitPeriod = timeSpanToEndOfMonth + TimeSpan.FromSeconds(OneHourWaitPeriodInSeconds);
                            await Task.Delay(waitPeriod, stoppingToken);
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromSeconds(OneHourWaitPeriodInSeconds), stoppingToken);
                        }
                    }
                    while (!stoppingToken.IsCancellationRequested);
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
                logger.Error(ex, "Background, usage calculator error.");
            }
        }

        private int OneHourWaitPeriodInSeconds => 60 * 60;

        private int OneDayLifetimeInSeconds => 60 * 60 * 24;

        private int TwoMonthLifetimeInSeconds => 60 * 60 * 24 * 62;

        private async Task<bool> DoWorkAsync(CancellationToken stoppingToken)
        {
            var now = DateTime.Now;
            var datePointer = new DateOnly(now.Year, now.Month, 1).AddMonths(-1);
            if (await cacheProvider.ExistsAsync(UsageMonthDoneKey(datePointer)))
            {
                return true;
            }

            if (!await cacheProvider.ExistsAsync(UsageDoWorkKey(datePointer)) && !await cacheProvider.ExistsAsync(UsageDoWorkWaitKey(datePointer)))
            {
                try
                {
                    var myId = Guid.NewGuid().ToString();
                    await cacheProvider.SetAsync(UsageDoWorkKey(datePointer), myId, OneDayLifetimeInSeconds);

                    // wait, for others to override
                    await Task.Delay(1000 * 10, stoppingToken); // 10 seconds

                    var keyId = await cacheProvider.GetAsync(UsageDoWorkKey(datePointer));
                    if (myId == keyId)
                    {
                        if (await cacheProvider.ExistsAsync(UsageMonthCalculatedKey(datePointer)))
                        {
                            return await DoWorkByUsedAsync(datePointer, stoppingToken);
                        }
                        else
                        {
                            return await DoWorkByTenantAsync(datePointer, stoppingToken);
                        }
                    }
                }
                finally
                {
                    await cacheProvider.DeleteAsync(UsageDoWorkKey(datePointer));
                    await cacheProvider.SetFlagAsync(UsageDoWorkWaitKey(datePointer), settings.Usage.BackgroundServiceWaitPeriod);
                }
            }
            return false;
        }

        private async Task<bool> DoWorkByTenantAsync(DateOnly datePointer, CancellationToken stoppingToken)
        {
            (var calculatonTasksDone, var invoicingTasksDone) = await DoWorkScopeByTenantAsync(datePointer, stoppingToken);
            if (calculatonTasksDone && invoicingTasksDone)
            {
                await cacheProvider.SetFlagAsync(UsageMonthDoneKey(datePointer), TwoMonthLifetimeInSeconds);
                return true;
            }
            else if (calculatonTasksDone)
            {
                await cacheProvider.SetFlagAsync(UsageMonthCalculatedKey(datePointer), TwoMonthLifetimeInSeconds);
            }
            return false;
        }

        private async Task<bool> DoWorkByUsedAsync(DateOnly datePointer, CancellationToken stoppingToken)
        {
            var tasksDone = await DoWorkScopeByUsedAsync(stoppingToken);
            if (tasksDone)
            {
                await cacheProvider.SetFlagAsync(UsageMonthDoneKey(datePointer), TwoMonthLifetimeInSeconds);
                return true;
            }
            return false;
        }

        private async Task<(bool calculatonTasksDone, bool invoicingTasksDone)> DoWorkScopeByTenantAsync(DateOnly datePointer, CancellationToken stoppingToken)
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<TelemetryScopedLogger>();
                try
                {
                    var calculatonTasksDone = true;
                    var invoicingTasksDone = true;
                    scopedLogger.SetScopeProperty(Constants.Logs.TenantName, Constants.Routes.MasterTenantName);
                    scopedLogger.SetScopeProperty(Constants.Logs.TrackName, Constants.Routes.MasterTrackName);
                    scopedLogger.Event("Usage calculation and invoicing stated by list of tenants.");

                    var tenantDataRepository = scope.ServiceProvider.GetRequiredService<ITenantDataRepository>();
                    string paginationToken = null;
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        (var tenants, paginationToken) = await tenantDataRepository.GetListAsync<Tenant>(whereQuery: t => !string.IsNullOrEmpty(t.PlanName) && t.PlanName != "free", pageSize: 10, paginationToken: paginationToken);
                        foreach (var tenant in tenants)
                        {
                            try
                            {
                                stoppingToken.ThrowIfCancellationRequested();
                                (var calculatonTaskDone, var used) = await scope.ServiceProvider.GetService<UsageCalculatorLogic>().DoCalculationAsync(datePointer, tenant, stoppingToken);
                                if (calculatonTaskDone)
                                {
                                    stoppingToken.ThrowIfCancellationRequested();
                                    if (!await scope.ServiceProvider.GetService<UsageInvoicingLogic>().DoInvoicingAsync(tenant, used, stoppingToken))
                                    {
                                        invoicingTasksDone = false;
                                    }
                                }
                                else
                                {
                                    calculatonTasksDone = false;
                                    invoicingTasksDone = false;
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
                            catch (Exception tEx)
                            {
                                scopedLogger.Error(tEx, $"Background worker, usage tenant '{tenant.Name}' calculation or invoicing error.");
                            }
                        }

                        if (paginationToken == null)
                        {
                            break;
                        }

                        // sleep to not use to many resources
                        await Task.Delay(new TimeSpan(0, 0, 5), stoppingToken);
                    }

                    if (calculatonTasksDone)
                    {
                        scopedLogger.Event("Usage calculation done.");
                    }
                    else
                    {
                        scopedLogger.Event("Usage calculation NOT done.");
                    }
                    if (invoicingTasksDone)
                    {
                        scopedLogger.Event("Usage invoicing done.");
                    }
                    else
                    {
                        scopedLogger.Event("Usage invoicing NOT done.");
                    }

                    return (calculatonTasksDone, invoicingTasksDone);
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
                    scopedLogger.Error(ex, "Background worker, usage calculation and invoicing error.");
                    return (false, false);
                }
            }
        }

        private async Task<bool> DoWorkScopeByUsedAsync(CancellationToken stoppingToken)
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<TelemetryScopedLogger>();
                try
                {
                    var tasksDone = true;
                    scopedLogger.SetScopeProperty(Constants.Logs.TenantName, Constants.Routes.MasterTenantName);
                    scopedLogger.SetScopeProperty(Constants.Logs.TrackName, Constants.Routes.MasterTrackName);
                    scopedLogger.Event("Usage invoicing stated by list of used items.");

                    var tenantDataRepository = scope.ServiceProvider.GetRequiredService<ITenantDataRepository>();
                    string paginationToken = null;
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        (var usedList, paginationToken) = await tenantDataRepository.GetListAsync<Used>(whereQuery: u => !u.IsDone, pageSize: 10, paginationToken: paginationToken);
                        foreach (var used in usedList)
                        {
                            try
                            {
                                stoppingToken.ThrowIfCancellationRequested();
                                var tenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(used.TenantName));
                                if (!await scope.ServiceProvider.GetService<UsageInvoicingLogic>().DoInvoicingAsync(tenant, used, stoppingToken))
                                {
                                    tasksDone = false;
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
                            catch (Exception tEx)
                            {
                                scopedLogger.Error(tEx, $"Background worker, usage tenant '{used.TenantName}' invoicing error.");
                            }
                        }

                        if (paginationToken == null)
                        {
                            break;
                        }

                        // sleep to not use to many resources
                        await Task.Delay(new TimeSpan(0, 0, 5), stoppingToken);
                    }

                    if (tasksDone)
                    {
                        scopedLogger.Event("Usage invoicing done.");
                    }
                    else
                    {
                        scopedLogger.Event("Usage invoicing NOT done.");
                    }

                    return tasksDone;
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
                    scopedLogger.Error(ex, "Background worker, usage calculation and invoicing error.");
                    return false;
                }
            }
        }

        private string UsageDoWorkKey(DateOnly datePointer)
        {
            return $"usage_do_work_{SubKey(datePointer)}";
        }

        private string UsageDoWorkWaitKey(DateOnly datePointer)
        {
            return $"usage_do_work_wait_{SubKey(datePointer)}";
        }

        private string UsageMonthCalculatedKey(DateOnly datePointer)
        {
            return $"usage_month_calculated_{SubKey(datePointer)}";
        }

        private string UsageMonthDoneKey(DateOnly datePointer)
        {
            return $"usage_month_done_{SubKey(datePointer)}";
        }


        private string SubKey(DateOnly datePointer)
        {
            return $"{datePointer.Year}-{datePointer.Month}";
        }
    }
}
