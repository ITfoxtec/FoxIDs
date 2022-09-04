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
            if (RouteBindingDoNotExists()) return null;
            CheckRouteBinding();

            logger.ScopeTrace(() => $"Get track cookie '{typeof(TMessage).Name}', route '{RouteBinding.Route}'.");

            var cookie = httpContextAccessor.HttpContext.Request.Cookies[CookieName()];
            if (!cookie.IsNullOrWhiteSpace())
            {
                try
                {
                    var envelope = CookieEnvelope<TMessage>.FromCookieString(CreateProtector(), cookie);
                    return envelope.Message;
                }
                catch (CryptographicException ex)
                {
                    logger.Warning(ex, $"Unable to unprotect track cookie '{typeof(TMessage).Name}', deleting cookie.");
                    DeleteByName(CookieName());
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
            CheckRouteBinding();
            if (message == null) new ArgumentNullException(nameof(message));

            logger.ScopeTrace(() => $"Save track cookie '{typeof(TMessage).Name}', route '{RouteBinding.Route}'.");

            var cookieOptions = new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = message.SameSite,
                IsEssential = true,
                Path = GetPath(),
            };

            httpContextAccessor.HttpContext.Response.Cookies.Append(
                CookieName(),
                new CookieEnvelope<TMessage>
                {
                    Message = message,
                }.ToCookieString(CreateProtector()),
                cookieOptions);
        }

        private void Delete()
        {
            if (RouteBindingDoNotExists()) return;
            CheckRouteBinding();

            logger.ScopeTrace(() => $"Delete track cookie '{typeof(TMessage).Name}', route '{RouteBinding.Route}'.");

            DeleteByName(CookieName());
        }

        private void CheckRouteBinding()
        {
            if (RouteBinding == null) new ArgumentNullException(nameof(RouteBinding));
            if (RouteBinding.TenantName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(RouteBinding.TenantName), RouteBinding.GetTypeName());
            if (RouteBinding.TrackName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(RouteBinding.TrackName), RouteBinding.GetTypeName());
        }

        private bool RouteBindingDoNotExists()
        {
            if (RouteBinding == null) return true;
            if (RouteBinding.TenantName.IsNullOrEmpty()) return true;
            if (RouteBinding.TrackName.IsNullOrEmpty()) return true;

            return false;
        }

        private void DeleteByName(string name)
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
                    Path = GetPath(),
                });
        }

        private string GetPath()
        {
            return $"/{RouteBinding.TenantName}/{RouteBinding.TrackName}";
        }

        private IDataProtector CreateProtector()
        {
            return dataProtection.CreateProtector(new[] { RouteBinding.TenantName, RouteBinding.TrackName });
        }

        private string CookieName()
        {
            return typeof(TMessage).Name.ToLower();
        }

        private RouteBinding RouteBinding => httpContextAccessor.HttpContext.GetRouteBinding();
    }
}
