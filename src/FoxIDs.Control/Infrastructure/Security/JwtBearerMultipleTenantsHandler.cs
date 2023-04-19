using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Authentication;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using ITfoxtec.Identity.Util;
using ITfoxtec.Identity.Tokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Linq;

namespace FoxIDs.Infrastructure.Security
{
    public class JwtBearerMultipleTenantsHandler : AuthenticationHandler<JwtBearerMultipleTenantsOptions>
    {
        public const string AuthenticationScheme = "bearer-multiple-tenants";

        public JwtBearerMultipleTenantsHandler(IOptionsMonitor<JwtBearerMultipleTenantsOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                var accessToken = GetAccessTokenFromHeader();

                var routeBinding = Context.GetRouteBinding();
                var authority = UrlCombine.Combine(Options.FoxIDsEndpoint, routeBinding.TenantName, Constants.Routes.MasterTrackName, Options.DownParty);

                var oidcDiscoveryUri = UrlCombine.Combine(authority, IdentityConstants.OidcDiscovery.Path);
                var oidcDiscoveryHandler = Context.RequestServices.GetService<OidcDiscoveryHandlerService>();
                var oidcDiscovery = await oidcDiscoveryHandler.GetOidcDiscoveryAsync(oidcDiscoveryUri);
                var oidcDiscoveryKeySet = await oidcDiscoveryHandler.GetOidcDiscoveryKeysAsync(oidcDiscoveryUri);

                if(oidcDiscoveryKeySet.Keys?.Count() < 1)
                {
                    try
                    {
                        throw new Exception($"OIDC discovery keys is empty, reload keys, Uri '{oidcDiscoveryUri}'.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, ex.Message);
                        oidcDiscoveryKeySet = await oidcDiscoveryHandler.GetOidcDiscoveryKeysAsync(oidcDiscoveryUri, refreshCache: true);
                    }
                }

                ClaimsPrincipal principal;
                try
                {
                    (principal, _) = JwtHandler.ValidateToken(accessToken, oidcDiscovery.Issuer, oidcDiscoveryKeySet.Keys, Options.DownParty);
                }
                catch (SecurityTokenInvalidSignatureException isex)
                {
                    Logger.LogWarning(isex, $"Invalid signature reload OIDC discovery keys, Uri '{oidcDiscoveryUri}'.");
                    oidcDiscoveryKeySet = await oidcDiscoveryHandler.GetOidcDiscoveryKeysAsync(oidcDiscoveryUri, refreshCache: true);
                    (principal, _) = JwtHandler.ValidateToken(accessToken, oidcDiscovery.Issuer, oidcDiscoveryKeySet.Keys, Options.DownParty);
                }

                var logger = Context.RequestServices.GetService<TelemetryScopedLogger>();
                logger.SetUserScopeProperty(principal.Claims);

                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return AuthenticateResult.Fail(ex.Message);
            }
        }

        private string GetAccessTokenFromHeader()
        {
            var accessToken = Request.Headers.GetAuthorizationHeaderBearer();

            if (accessToken.IsNullOrWhiteSpace())
            {
                throw new AuthenticationException("Authorization header token is empty.");
            }

            return accessToken;
        }
    }
}
