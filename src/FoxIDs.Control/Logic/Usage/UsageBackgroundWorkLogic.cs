using FoxIDs.Infrastructure;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Usage
{
    public class UsageBackgroundWorkLogic : LogicBase
    {
        private const int oneDayLifetimeInSeconds = 60 * 60 * 24;
        private const int twoMonthLifetimeInSeconds = 60 * 60 * 24 * 62;
        private const int waitForOthersToRunInSeconds = 10;
        private const int notUseToManyResourcesInSeconds = 5;
        private const int loadPageSize = 10;

        private readonly TelemetryScopedLogger scopedLogger;
        private readonly FoxIDsControlSettings settings;
        private readonly ICacheProvider cacheProvider;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly UsageCalculatorLogic usageCalculatorLogic;
        private readonly UsageInvoicingLogic usageInvoicingLogic;

        public UsageBackgroundWorkLogic(TelemetryScopedLogger scopedLogger, FoxIDsControlSettings settings, ICacheProvider cacheProvider, ITenantDataRepository tenantDataRepository, UsageCalculatorLogic usageCalculatorLogic, UsageInvoicingLogic usageInvoicingLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.scopedLogger = scopedLogger;
            this.settings = settings;
            this.cacheProvider = cacheProvider;
            this.tenantDataRepository = tenantDataRepository;
            this.usageCalculatorLogic = usageCalculatorLogic;
            this.usageInvoicingLogic = usageInvoicingLogic;

            var now = DateTime.Now;
            DatePointer = new DateOnly(now.Year, now.Month, 1).AddMonths(-1);
        }

        public async Task<bool> DoWorkAsync(CancellationToken stoppingToken)
        {
            if (await CacheExistsAsync(UsageMonthDoneKey))
            {
                return true;
            }

            if (!await CacheExistsAsync(UsageDoWorkKey) && !await CacheExistsAsync(UsageDoWorkWaitKey))
            {
                var myId = Guid.NewGuid().ToString();
                await CacheSetAsync(UsageDoWorkKey, myId, oneDayLifetimeInSeconds);

                // wait, for others to override
                await Task.Delay(1000 * waitForOthersToRunInSeconds, stoppingToken); // 10 seconds

                var keyId = await CacheGetAsync(UsageDoWorkKey);
                if (myId == keyId)
                {
                    var tasksDone = false;
                    if (!await CacheExistsAsync(UsageMonthCalculatedKey))
                    {
                        (var calculatonTasksDone, var invoicingTasksDone) = await DoWorkInnerByTenantAsync(stoppingToken);
                        if (calculatonTasksDone)
                        {
                            await CacheSetFlagAsync(UsageMonthCalculatedKey, twoMonthLifetimeInSeconds);
                        }
                        if (calculatonTasksDone && invoicingTasksDone)
                        {
                            tasksDone = true;
                        }
                    }
                    else
                    {
                        if (await DoWorkInnerByUsedAsync(stoppingToken))
                        {
                            tasksDone = true;
                        }
                    }

                    if(tasksDone)
                    {
                        await CacheSetFlagAsync(UsageMonthDoneKey, twoMonthLifetimeInSeconds);
                    }
                    else
                    {
                        await CacheSetFlagAsync(UsageDoWorkWaitKey, settings.Usage.BackgroundServiceWaitPeriod);
                    }

                    await CacheDeleteAsync(UsageDoWorkKey);
                    return tasksDone;
                }
            }
            return false;
        }

        private async Task<bool> CacheExistsAsync(string key)
        {
            var exist = await cacheProvider.ExistsAsync(key);
            scopedLogger.Event($"Usage, do work, cache key '{key}' {(exist ? string.Empty : "NOT ")}exist.");
            return exist;
        }

        private async Task CacheSetAsync(string key, string value, int lifetime)
        {
            await cacheProvider.SetAsync(key, value, lifetime);
            scopedLogger.Event($"Usage, do work, set cache key '{key}' value '{value}' with lifetime {lifetime}.");
        }

        private async Task CacheSetFlagAsync(string key, int lifetime)
        {
            await cacheProvider.SetFlagAsync(key, lifetime);
            scopedLogger.Event($"Usage, do work, set flag cache key '{key}' with lifetime {lifetime}.");
        }

        private async Task<string> CacheGetAsync(string key)
        {
            var value = await cacheProvider.GetAsync(key);
            scopedLogger.Event($"Usage, do work, get cache key '{key}' has value '{value}'.");
            return value;
        }

        private async Task CacheDeleteAsync(string key)
        {
            await cacheProvider.DeleteAsync(key);
            scopedLogger.Event($"Usage, do work, delete cache key '{key}'.");
        }

        private async Task<(bool calculatonTasksDone, bool invoicingTasksDone)> DoWorkInnerByTenantAsync(CancellationToken stoppingToken)
        {
            try
            {
                var calculatonTasksDone = false;
                var invoicingTasksDone = false;                 
                scopedLogger.Event("Usage, calculation and invoicing stated by query tenants.");

                string paginationToken = null;
                while (!stoppingToken.IsCancellationRequested)
                {
                    (var tenants, paginationToken) = await tenantDataRepository.GetListAsync<Tenant>(whereQuery: t => !string.IsNullOrEmpty(t.PlanName) && t.PlanName != "free", pageSize: loadPageSize, paginationToken: paginationToken);
                    foreach (var tenant in tenants)
                    {
                        try
                        {
                            stoppingToken.ThrowIfCancellationRequested();
                            var used = await usageCalculatorLogic.DoCalculationAsync(DatePointer, tenant, stoppingToken);
                            calculatonTasksDone = true;

                            stoppingToken.ThrowIfCancellationRequested();
                            if (await usageInvoicingLogic.DoInvoicingAsync(tenant, used, stoppingToken))
                            {
                                invoicingTasksDone = true;
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
                            scopedLogger.Error(tEx, $"Usage, calculation and invoicing for tenant '{tenant.Name}' error.");
                        }
                    }

                    if (paginationToken == null)
                    {
                        break;
                    }

                    // sleep to not use to many resources
                    await Task.Delay(new TimeSpan(0, 0, notUseToManyResourcesInSeconds), stoppingToken);
                }

                if (calculatonTasksDone)
                {
                    scopedLogger.Event("Usage, calculation done.");
                }
                else
                {
                    scopedLogger.Event("Usage, calculation NOT done.");
                }
                if (invoicingTasksDone)
                {
                    scopedLogger.Event("Usage, invoicing done.");
                }
                else
                {
                    scopedLogger.Event("Usage, invoicing NOT done.");
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
                scopedLogger.Error(ex, "Usage, calculation and invoicing error.");
                return (false, false);
            }
        }

        private async Task<bool> DoWorkInnerByUsedAsync(CancellationToken stoppingToken)
        {
            try
            {
                var invoicingTasksDone = false;
                scopedLogger.Event("Usage, invoicing stated by query used items.");

                string paginationToken = null;
                while (!stoppingToken.IsCancellationRequested)
                {
                    (var usedList, paginationToken) = await tenantDataRepository.GetListAsync<Used>(whereQuery: u => !u.IsDone, pageSize: loadPageSize, paginationToken: paginationToken);
                    foreach (var used in usedList)
                    {
                        try
                        {
                            stoppingToken.ThrowIfCancellationRequested();
                            var tenant = await tenantDataRepository.GetAsync<Tenant>(await Tenant.IdFormatAsync(used.TenantName));
                            if (await usageInvoicingLogic.DoInvoicingAsync(tenant, used, stoppingToken))
                            {
                                invoicingTasksDone = true;
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
                            scopedLogger.Error(tEx, $"Usage, invoicing for tenant '{used.TenantName}' error.");
                        }
                    }

                    if (paginationToken == null)
                    {
                        break;
                    }

                    // sleep to not use to many resources
                    await Task.Delay(new TimeSpan(0, 0, notUseToManyResourcesInSeconds), stoppingToken);
                }

                if (invoicingTasksDone)
                {
                    scopedLogger.Event("Usage, invoicing done.");
                }
                else
                {
                    scopedLogger.Event("Usage, invoicing NOT done.");
                }

                return invoicingTasksDone;
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
                scopedLogger.Error(ex, "Usage, invoicing for tenant error.");
                return false;
            }
        }

        private DateOnly DatePointer { get; init; }

        private string UsageDoWorkKey => $"usage_do_work_{SubKey}";

        private string UsageDoWorkWaitKey => $"usage_do_work_wait_{SubKey}";

        private string UsageMonthCalculatedKey => $"usage_month_calculated_{SubKey}";

        private string UsageMonthDoneKey => $"usage_month_done_{SubKey}";

        private string SubKey => $"y:{DatePointer.Year}-m:{DatePointer.Month}";
    }
}
