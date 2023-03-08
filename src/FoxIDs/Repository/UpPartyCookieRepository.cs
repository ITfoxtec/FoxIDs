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
    public class UpPartyCookieRepository<TMessage> where TMessage : CookieMessage, new()
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IDataProtectionProvider dataProtection;
        private readonly IHttpContextAccessor httpContextAccessor;

        public UpPartyCookieRepository(TelemetryScopedLogger logger, IDataProtectionProvider dataProtection, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.dataProtection = dataProtection;
            this.httpContextAccessor = httpContextAccessor;
        }

        public Task<TMessage> GetAsync(UpParty party, bool delete = false, bool tryGet = false)
        {
            return Task.FromResult(Get(party, delete, tryGet));
        }

        public Task SaveAsync(UpParty party, TMessage message, DateTimeOffset? persistentCookieExpires)
        {
            Save(party, message, persistentCookieExpires);
            return Task.FromResult(0);
        }

        public Task DeleteAsync(UpParty party, bool tryDelete = false)
        {
            Delete(party, tryDelete);
            return Task.FromResult(0);
        }

        private TMessage Get(UpParty party, bool delete, bool tryGet = false)
        {
            var routeBinding = GetRouteBinding();
            if (tryGet && RouteBindingDoNotExists(routeBinding)) return null;
            CheckRouteBinding(routeBinding);

            logger.ScopeTrace(() => $"Get up-party cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}', delete '{delete}'.");

            var cookie = httpContextAccessor.HttpContext.Request.Cookies[CookieName()];
            if (!cookie.IsNullOrWhiteSpace())
            {
                try
                {
                    var envelope = CookieEnvelope<TMessage>.FromCookieString(CreateProtector(routeBinding), cookie);

                    if (delete)
                    {
                        logger.ScopeTrace(() => $"Delete up-party cookie, '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");
                        DeleteByName(routeBinding, party, CookieName());
                    }

                    return envelope.Message;
                }
                catch (CryptographicException ex)
                {
                    logger.Warning(ex, $"Unable to unprotect up-party cookie '{typeof(TMessage).Name}', deleting cookie.");
                    DeleteByName(routeBinding, party, CookieName());
                    return null;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to read up-party cookie '{typeof(TMessage).Name}'.", ex);
                }
            }
            else
            {
                return null;
            }
        }

        private void Save(UpParty party, TMessage message, DateTimeOffset? persistentCookieExpires)
        {
            var routeBinding = GetRouteBinding();
            CheckRouteBinding(routeBinding);
            if (message == null) new ArgumentNullException(nameof(message));

            logger.ScopeTrace(() => $"Save up-party cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

            var cookieOptions = new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = message.SameSite,
                IsEssential = true,
                Path = GetPath(routeBinding, party),
            };
            if (persistentCookieExpires != null)
            {
                cookieOptions.Expires = persistentCookieExpires;
            }

            httpContextAccessor.HttpContext.Response.Cookies.Append(
                CookieName(),
                new CookieEnvelope<TMessage>
                {
                    Message = message,
                }.ToCookieString(CreateProtector(routeBinding)),
                cookieOptions);
        }

        private void Delete(UpParty party, bool tryDelete = false)
        {
            var routeBinding = GetRouteBinding();
            if (tryDelete && RouteBindingDoNotExists(routeBinding)) return;
            CheckRouteBinding(routeBinding);

            logger.ScopeTrace(() => $"Delete up-party cookie '{typeof(TMessage).Name}', route '{routeBinding.Route}'.");

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

        private void DeleteByName(RouteBinding routeBinding, UpParty party, string name)
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
                    Path = GetPath(routeBinding, party),
                });
        }

        private string GetPath(RouteBinding routeBinding, UpParty party)
        {
            return $"{(!routeBinding.HasCustomDomain ? $"/{routeBinding.TenantName}" : string.Empty)}/{routeBinding.TrackName}/{routeBinding.UpParty.Name.ToUpPartyBinding(party.PartyBindingPattern)}";
        }

        private IDataProtector CreateProtector(RouteBinding routeBinding)
        {
            return dataProtection.CreateProtector(new[] { routeBinding.TenantName, routeBinding.TrackName, routeBinding.UpParty.Name, typeof(TMessage).Name });
        }

        private string CookieName()
        {
            return typeof(TMessage).Name.ToLower();
        }

        private RouteBinding GetRouteBinding() => httpContextAccessor.HttpContext.GetRouteBinding();
    }
}
