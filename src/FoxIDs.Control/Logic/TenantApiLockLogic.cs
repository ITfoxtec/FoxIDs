using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class TenantApiLockLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantDataRepository tenantDataRepository;

        public TenantApiLockLogic(TelemetryScopedLogger logger, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task<TenantApiLock> AcquireAsync(string tenantName, string scope)
        {
            if (tenantName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(tenantName));
            if (scope.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(scope));

            var lockIdKey = new TenantApiLock.IdKey { TenantName = tenantName, Scope = scope };
            var tenantLock = new TenantApiLock
            {
                Id = await TenantApiLock.IdFormatAsync(lockIdKey),
                TenantName = tenantName,
                Scope = scope,
                TimeToLive = TenantApiLock.DefaultLifetimeSeconds,
                RequestId = HttpContext.TraceIdentifier
            };

            var cancellationToken = HttpContext.RequestAborted;
            for (var attempt = 0; attempt < TenantApiLock.MaxAcquireAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await tenantDataRepository.CreateAsync(tenantLock, logger);
                    logger.ScopeTrace(() => $"Tenant '{tenantName}' and scope '{scope}' API lock '{tenantLock.Id}' acquired.");
                    return tenantLock;
                }
                catch (FoxIDsDataException ex) when (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.ScopeTrace(() => $"Tenant '{tenantName}' and scope '{scope}' API lock is held by another request. Waiting to retry ({attempt + 1}/{TenantApiLock.MaxAcquireAttempts}).");
                    await Task.Delay(TenantApiLock.AcquireRetryDelay, cancellationToken);
                }
            }

            logger.ScopeTrace(() => $"Tenant '{tenantName}' and scope '{scope}' API lock could not be acquired after {TenantApiLock.MaxAcquireAttempts} attempts.");
            return null;
        }

        public async Task ReleaseAsync(TenantApiLock tenantLock)
        {
            if (tenantLock == null)
            {
                return;
            }

            try
            {
                await tenantDataRepository.DeleteAsync<TenantApiLock>(tenantLock.Id, scopedLogger: logger);
                logger.ScopeTrace(() => $"Tenant '{tenantLock.TenantName}' and scope '{tenantLock.Scope}' API lock '{tenantLock.Id}' released.");
            }
            catch (FoxIDsDataException ex) when (ex.StatusCode == DataStatusCode.NotFound)
            {
                logger.ScopeTrace(() => $"Tenant '{tenantLock.TenantName}' and scope '{tenantLock.Scope}' API lock '{tenantLock.Id}' already released or expired.");
            }
        }
    }
}
