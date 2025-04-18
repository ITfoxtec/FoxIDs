using ITfoxtec.Identity;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Session;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace FoxIDs.Repository
{
    public class UpPartyCookieRepository<TMessage> where TMessage : CookieMessage, new()
    {
        private ConcurrentDictionary<string, TMessage> cookieCache = new ConcurrentDictionary<string, TMessage>();
        private readonly TelemetryScopedLogger logger;
        private readonly IDataProtectionProvider dataProtection;
        private readonly IHttpContextAccessor httpContextAccessor;

        public UpPartyCookieRepository(TelemetryScopedLogger logger, IDataProtectionProvider dataProtection, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.dataProtection = dataProtection;
            this.httpContextAccessor = httpContextAccessor;
        }

        public Task<TMessage> GetAsync(IUpParty party, bool delete = false, bool tryGet = false)
        {
            return Task.FromResult(Get(party, delete, tryGet));
        }

        public Task SaveAsync(IUpParty party, TMessage message, DateTimeOffset? persistentCookieExpires)
        {
            Save(party, message, persistentCookieExpires);
            return Task.FromResult(0);
        }

        public Task DeleteAsync(IUpParty party, bool tryDelete = false)
        {
            Delete(party, tryDelete);
            return Task.FromResult(0);
        }

        private TMessage Get(IUpParty party, bool delete, bool tryGet = false)
        {
            var routeBinding = GetRouteBinding();
            if (tryGet && RouteBindingDoNotExists(routeBinding)) return null;
            CheckRouteBinding(routeBinding);

            logger.ScopeTrace(() => $"Get authentication method cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}', delete '{delete}'.");

            var cookieName = CookieName();
            if (cookieCache.TryGetValue(CookieName(), out TMessage cacheCookie))
            {
                return cacheCookie;
            }

            var cookie = httpContextAccessor.HttpContext.Request.Cookies[cookieName];
            if (!cookie.IsNullOrWhiteSpace())
            {
                try
                {
                    var envelope = CookieEnvelope<TMessage>.FromCookieString(CreateProtector(routeBinding), cookie);

                    if (delete)
                    {
                        logger.ScopeTrace(() => $"Delete authentication method cookie, '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");
                        DeleteByName(routeBinding, party, cookieName);
                    }

                    return envelope.Message;
                }
                catch (CryptographicException ex)
                {
                    logger.Warning(ex, $"Unable to unprotect authentication method cookie '{typeof(TMessage).Name}', deleting cookie.");
                    DeleteByName(routeBinding, party, cookieName);
                    return null;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to read authentication method cookie '{typeof(TMessage).Name}'.", ex);
                }
            }
            else
            {
                return null;
            }
        }

        private void Save(IUpParty party, TMessage message, DateTimeOffset? persistentCookieExpires)
        {
            var routeBinding = GetRouteBinding();
            CheckRouteBinding(routeBinding);
            if (message == null) new ArgumentNullException(nameof(message));

            logger.ScopeTrace(() => $"Save authentication method cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

            var cookieName = CookieName();
            cookieCache[cookieName] = message;

            httpContextAccessor.HttpContext.Response.Headers.SetCookie = httpContextAccessor.HttpContext.Response.Headers.SetCookie.Where(c => !c.StartsWith($"{cookieName}=")).ToArray();
            var cookieOptions = new CookieOptions
            {
                Secure = httpContextAccessor.HttpContext.Request.Scheme != Uri.UriSchemeHttp,
                HttpOnly = true,
                SameSite = message.SameSite,
                IsEssential = true,
                Path = GetPath(routeBinding, party),
                Expires = persistentCookieExpires
            };
            httpContextAccessor.HttpContext.Response.Cookies.Append(
                cookieName,
                new CookieEnvelope<TMessage>
                {
                    Message = message,
                }.ToCookieString(CreateProtector(routeBinding)),
                cookieOptions);
        }

        private void Delete(IUpParty party, bool tryDelete = false)
        {
            var routeBinding = GetRouteBinding();
            if (tryDelete && RouteBindingDoNotExists(routeBinding)) return;
            CheckRouteBinding(routeBinding);

            logger.ScopeTrace(() => $"Delete authentication method cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

            DeleteByName(routeBinding, party, CookieName());
        }

        private void CheckRouteBinding(RouteBinding routeBinding)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (routeBinding.TenantName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(routeBinding.TenantName), routeBinding.GetTypeName());
            if (routeBinding.TrackName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(routeBinding.TrackName), routeBinding.GetTypeName());
            if (routeBinding.UpParty == null) throw new ArgumentNullException(nameof(routeBinding.UpParty), routeBinding.GetTypeName());
        }

        private bool RouteBindingDoNotExists(RouteBinding routeBinding)
        {
            if (routeBinding == null) return true;
            if (routeBinding.TenantName.IsNullOrEmpty()) return true;
            if (routeBinding.TrackName.IsNullOrEmpty()) return true;
            if (routeBinding.UpParty == null) return true;

            return false;
        }

        private void DeleteByName(RouteBinding routeBinding, IUpParty party, string name)
        {
            cookieCache.TryRemove(name, out TMessage cacheCookie);

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
                    Path = GetPath(routeBinding, party),
                });
        }

        private string GetPath(RouteBinding routeBinding, IUpParty party)
        {
            return $"{(!routeBinding.UseCustomDomain ? $"/{routeBinding.TenantName}" : string.Empty)}/{routeBinding.TrackName}/{routeBinding.UpParty.Name.ToUpPartyBinding(party.PartyBindingPattern)}";
        }

        private IDataProtector CreateProtector(RouteBinding routeBinding)
        {
            return dataProtection.CreateProtector([routeBinding.TenantName, routeBinding.TrackName, routeBinding.UpParty.Name, typeof(TMessage).Name]);
        }

        private string CookieName()
        {
            return typeof(TMessage).Name.ToLower();
        }

        private RouteBinding GetRouteBinding() => httpContextAccessor.HttpContext.GetRouteBinding();
    }
}
