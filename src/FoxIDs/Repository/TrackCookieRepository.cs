using ITfoxtec.Identity;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Session;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class TrackCookieRepository<TMessage> where TMessage : CookieMessage, new()
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IDataProtectionProvider dataProtection;
        private readonly IHttpContextAccessor httpContextAccessor;

        public TrackCookieRepository(TelemetryScopedLogger logger, IDataProtectionProvider dataProtection, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.dataProtection = dataProtection;
            this.httpContextAccessor = httpContextAccessor;
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

            logger.ScopeTrace(() => $"Get track cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

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
                    logger.Warning(ex, $"Unable to unprotect track cookie '{typeof(TMessage).Name}', deleting cookie.");
                    DeleteByName(routeBinding, CookieName());
                    return null;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to read track cookie '{typeof(TMessage).Name}'.", ex);
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

            logger.ScopeTrace(() => $"Save track cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

            var cookieOptions = new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = message.SameSite,
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

            logger.ScopeTrace(() => $"Delete track cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

            DeleteByName(routeBinding, CookieName());
        }

        private void CheckRouteBinding(RouteBinding routeBinding)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (routeBinding.TenantName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(routeBinding.TenantName), routeBinding.GetTypeName());
            if (routeBinding.TrackName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(routeBinding.TrackName), routeBinding.GetTypeName());
        }

        private bool RouteBindingDoNotExists(RouteBinding routeBinding)
        {
            if (routeBinding == null) return true;
            if (routeBinding.TenantName.IsNullOrEmpty()) return true;
            if (routeBinding.TrackName.IsNullOrEmpty()) return true;

            return false;
        }

        private void DeleteByName(RouteBinding routeBinding, string name)
        {
            httpContextAccessor.HttpContext.Response.Cookies.Append(
                name,
                string.Empty,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMonths(-1),
                    Secure = true,
                    HttpOnly = true,
                    SameSite = new TMessage().SameSite,
                    IsEssential = true,
                    Path = GetPath(routeBinding),
                });
        }

        private string GetPath(RouteBinding routeBinding)
        {
            return $"{(!routeBinding.HasCustomDomain ? $"/{routeBinding.TenantName}" : string.Empty)}/{routeBinding.TrackName}";
        }

        private IDataProtector CreateProtector(RouteBinding routeBinding)
        {
            return dataProtection.CreateProtector(new[] { routeBinding.TenantName, routeBinding.TrackName });
        }

        private string CookieName()
        {
            return typeof(TMessage).Name.ToLower();
        }

        private RouteBinding GetRouteBinding() => httpContextAccessor.HttpContext.GetRouteBinding();
    }
}
