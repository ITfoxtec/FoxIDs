using ITfoxtec.Identity;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class SingleCookieRepository<TMessage> where TMessage : CookieMessage
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IDataProtectionProvider dataProtection;
        private readonly IHttpContextAccessor httpContextAccessor;

        public SingleCookieRepository(TelemetryScopedLogger logger, IDataProtectionProvider dataProtection, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.dataProtection = dataProtection;
            this.httpContextAccessor = httpContextAccessor;
        }

        public Task ValidateAsync()
        {
            Get(false);
            return Task.FromResult(0);
        }

        public Task<TMessage> GetAsync(bool delete = false)
        {
            return Task.FromResult(Get(delete));
        }

        public Task SaveAsync(TMessage message, DateTimeOffset? persistentCookieExpires)
        {
            Save(message, persistentCookieExpires);
            return Task.FromResult(0);
        }

        public Task DeleteAsync()
        {
            Delete();
            return Task.FromResult(0);
        }

        private TMessage Get(bool delete)
        {
            if (RouteBinding.TenantName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(RouteBinding.TenantName), RouteBinding.GetTypeName());
            if (RouteBinding.TrackName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(RouteBinding.TrackName), RouteBinding.GetTypeName());
            if (RouteBinding.UpParty == null) throw new ArgumentNullException(nameof(RouteBinding.UpParty), RouteBinding.GetTypeName());

            logger.ScopeTrace($"Get Cookie '{typeof(TMessage).Name}', Route '{RouteBinding.Route}', Delete '{delete}'.");

            var cookie = httpContextAccessor.HttpContext.Request.Cookies[CookieName()];
            if (!cookie.IsNullOrWhiteSpace())
            {
                try
                {
                    var envelope = CookieEnvelope<TMessage>.FromCookieString(CreateProtector(), cookie);

                    if (delete)
                    {
                        logger.ScopeTrace($"Delete Cookie, '{typeof(TMessage).Name}', Route '{RouteBinding.Route}'.");
                        DeleteByName(CookieName());
                    }

                    return envelope.Message;
                }
                catch (CryptographicException ex)
                {
                    throw new Exception($"Unable to Unprotect Cookie '{typeof(TMessage).Name}'.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to Read Cookie '{typeof(TMessage).Name}'.", ex);
                }
            }
            else
            {
                return null;
            }
        }

        private void Save(TMessage message, DateTimeOffset? persistentCookieExpires)
        {
            if (RouteBinding.TenantName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(RouteBinding.TenantName), RouteBinding.GetTypeName());
            if (RouteBinding.TrackName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(RouteBinding.TrackName), RouteBinding.GetTypeName());
            if (RouteBinding.UpParty == null) throw new ArgumentNullException(nameof(RouteBinding.UpParty), RouteBinding.GetTypeName());
            if (message == null) new ArgumentNullException(nameof(message));

            logger.ScopeTrace($"Save Cookie '{typeof(TMessage).Name}', Route '{RouteBinding.Route}'.");

            var cookieOptions = new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = GetPath(),
            };
            if(persistentCookieExpires != null)
            {
                cookieOptions.Expires = persistentCookieExpires;
            }

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
            if (RouteBinding == null) new ArgumentNullException(nameof(RouteBinding));
            if (RouteBinding.TenantName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(RouteBinding.TenantName), RouteBinding.GetTypeName());
            if (RouteBinding.TrackName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(RouteBinding.TrackName), RouteBinding.GetTypeName());
            if (RouteBinding.UpParty == null) throw new ArgumentNullException(nameof(RouteBinding.UpParty), RouteBinding.GetTypeName());

            logger.ScopeTrace($"Delete Cookie '{typeof(TMessage).Name}', Route '{RouteBinding.Route}'.");

            DeleteByName(CookieName());
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
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    Path = GetPath(),
                });
        }

        private string GetPath()
        {
            return $"/{RouteBinding.TenantName}/{RouteBinding.TrackName}/({RouteBinding.UpParty.Name})";
        }

        private IDataProtector CreateProtector()
        {
            return dataProtection.CreateProtector(new[] { RouteBinding.TenantName, RouteBinding.TrackName, RouteBinding.UpParty.Name, typeof(TMessage).Name });
        }

        private string CookieName()
        {
            return $"{RouteBinding.TenantName}-{RouteBinding.TrackName}-({RouteBinding.UpParty.Name})-{typeof(TMessage).Name.ToLower()}";
        }

        private RouteBinding RouteBinding => httpContextAccessor.HttpContext.GetRouteBinding();
    }
}
