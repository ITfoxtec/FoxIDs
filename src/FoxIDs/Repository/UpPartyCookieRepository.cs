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
    public class UpPartyCookieRepository<TMessage> : CookieRepositoryBase<TMessage> where TMessage : CookieMessage, new()
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IDataProtectionProvider dataProtection;

        public UpPartyCookieRepository(TelemetryScopedLogger logger, IDataProtectionProvider dataProtection, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.dataProtection = dataProtection;
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

                    if (delete)
                    {
                        logger.ScopeTrace(() => $"Delete authentication method cookie, '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");
                        DeleteByName(routeBinding, party);
                    }

                    return envelope.Message;
                }
                catch (CryptographicException ex)
                {
                    logger.Warning(ex, $"Unable to unprotect authentication method cookie '{typeof(TMessage).Name}', deleting cookie.");
                    DeleteByName(routeBinding, party);
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

            logger.ScopeTrace(() => $"Update authentication method cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

            SetCacheCookie(message);

            httpContextAccessor.HttpContext.Response.Headers.SetCookie = httpContextAccessor.HttpContext.Response.Headers.SetCookie.Where(c => !c.StartsWith($"{CookieName()}=")).ToArray();
            var cookieOptions = new CookieOptions
            {
                Secure = httpContextAccessor.HttpContext.Request.IsHttps,
                HttpOnly = true,
                SameSite = GetSameSite(message.SameSite),
                IsEssential = true,
                Path = GetPath(routeBinding, party),
                Expires = persistentCookieExpires
            };
            httpContextAccessor.HttpContext.Response.Cookies.Append(
                CookieName(),
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

            DeleteByName(routeBinding, party);
        }

        protected override void CheckRouteBinding(RouteBinding routeBinding)
        {
            base.CheckRouteBinding(routeBinding);
            if (routeBinding.UpParty == null) throw new ArgumentNullException(nameof(routeBinding.UpParty), routeBinding.GetTypeName());
        }

        protected override bool RouteBindingDoNotExists(RouteBinding routeBinding)
        {
            if (base.RouteBindingDoNotExists(routeBinding)) return true;
            if (routeBinding.UpParty == null) return true;

            return false;
        }

        private void DeleteByName(RouteBinding routeBinding, IUpParty party)
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
    }
}
