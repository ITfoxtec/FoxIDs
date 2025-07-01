using ITfoxtec.Identity;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Session;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;

namespace FoxIDs.Repository
{
    public class TrackCookieRepository<TMessage> : CookieRepositoryBase<TMessage> where TMessage : CookieMessage, new()
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IDataProtectionProvider dataProtection;

        public TrackCookieRepository(TelemetryScopedLogger logger, IDataProtectionProvider dataProtection, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.dataProtection = dataProtection;
        }

        public Task<TMessage> GetAsync()
        {
            return Task.FromResult(Get());
        }

        public Task SaveAsync(TMessage message)
        {
            Save(message);
            return Task.FromResult(0);
        }

        public Task DeleteAsync()
        {
            Delete();
            return Task.FromResult(0);
        }

        private TMessage Get()
        {
            var routeBinding = GetRouteBinding();
            if (RouteBindingDoNotExists(routeBinding)) return null;
            CheckRouteBinding(routeBinding);

            if (TryGetCacheCookie(out TMessage cacheCookie))
            {
                return cacheCookie;
            }

            var cookie = httpContextAccessor.HttpContext.Request.Cookies[CookieName()];
            if (!cookie.IsNullOrWhiteSpace())
            {
                try
                {
                    var envelope = CookieEnvelope<TMessage>.FromCookieString(CreateProtector(routeBinding), cookie);
                    return envelope.Message;
                }
                catch (CryptographicException ex)
                {
                    logger.Warning(ex, $"Unable to unprotect environment cookie '{typeof(TMessage).Name}', deleting cookie.");
                    DeleteByName(routeBinding);
                    return null;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to read environment cookie '{typeof(TMessage).Name}'.", ex);
                }
            }
            else
            {
                return null;
            }
        }

        private void Save(TMessage message)
        {
            var routeBinding = GetRouteBinding();
            CheckRouteBinding(routeBinding);
            if (message == null) new ArgumentNullException(nameof(message));

            logger.ScopeTrace(() => $"Update environment cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

            SetCacheCookie(message);

            httpContextAccessor.HttpContext.Response.Headers.SetCookie = httpContextAccessor.HttpContext.Response.Headers.SetCookie.Where(c => !c.StartsWith($"{CookieName()}=")).ToArray();
            var cookieOptions = new CookieOptions
            {
                Secure = httpContextAccessor.HttpContext.Request.IsHttps,
                HttpOnly = true,
                SameSite = GetSameSite(message.SameSite),
                IsEssential = true,
                Path = GetPath(routeBinding),
            };
            httpContextAccessor.HttpContext.Response.Cookies.Append(
                CookieName(),
                new CookieEnvelope<TMessage>
                {
                    Message = message,
                }.ToCookieString(CreateProtector(routeBinding)),
                cookieOptions);
        }

        private void Delete()
        {
            var routeBinding = GetRouteBinding();
            if (RouteBindingDoNotExists(routeBinding)) return;
            CheckRouteBinding(routeBinding);

            logger.ScopeTrace(() => $"Delete environment cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

            DeleteByName(routeBinding);
        }

        private void DeleteByName(RouteBinding routeBinding)
        {
            TryRemoveCacheCookie();

            httpContextAccessor.HttpContext.Response.Cookies.Append(
                CookieName(),
                string.Empty,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMonths(-1),
                    Secure = httpContextAccessor.HttpContext.Request.IsHttps,
                    HttpOnly = true,
                    SameSite = GetSameSite(new TMessage().SameSite),
                    IsEssential = true,
                    Path = GetPath(routeBinding),
                });
        }

        private string GetPath(RouteBinding routeBinding)
        {
            return $"{(!routeBinding.UseCustomDomain ? $"/{routeBinding.TenantName}" : string.Empty)}/{routeBinding.TrackName}";
        }

        private IDataProtector CreateProtector(RouteBinding routeBinding)
        {
            return dataProtection.CreateProtector([routeBinding.TenantName, routeBinding.TrackName]);
        }
    }
}
