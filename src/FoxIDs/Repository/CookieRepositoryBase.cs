using FoxIDs.Models;
using FoxIDs.Models.Session;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;

namespace FoxIDs.Repository
{
    public class CookieRepositoryBase<TMessage> where TMessage : CookieMessage, new()
    {
        private ConcurrentDictionary<string, TMessage> cookieCache = new ConcurrentDictionary<string, TMessage>();
        protected readonly IHttpContextAccessor httpContextAccessor;

        public CookieRepositoryBase(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        protected virtual void CheckRouteBinding(RouteBinding routeBinding)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (routeBinding.TenantName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(routeBinding.TenantName), routeBinding.GetTypeName());
            if (routeBinding.TrackName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(routeBinding.TrackName), routeBinding.GetTypeName());
        }

        protected virtual bool RouteBindingDoNotExists(RouteBinding routeBinding)
        {
            if (routeBinding == null) return true;
            if (routeBinding.TenantName.IsNullOrEmpty()) return true;
            if (routeBinding.TrackName.IsNullOrEmpty()) return true;

            return false;
        }

        protected SameSiteMode GetSameSite(SameSiteMode sameSite)
        {
            if (sameSite == SameSiteMode.None && !httpContextAccessor.HttpContext.Request.IsHttps)
            {
                return SameSiteMode.Lax;
            }
            return sameSite;
        }

        protected void SetCacheCookie(TMessage message)
        {
            cookieCache[CookieName()] = message;
        }

        protected bool TryGetCacheCookie(out TMessage message)
        {
            return cookieCache.TryGetValue(CookieName(), out message);
        }

        protected void TryRemoveCacheCookie()
        {
            _ = cookieCache.TryRemove(CookieName(), out TMessage message);
        }

        protected string CookieName()
        {
            return typeof(TMessage).Name.ToLower();
        }

        protected RouteBinding GetRouteBinding() => httpContextAccessor.HttpContext.GetRouteBinding();
    }
}
