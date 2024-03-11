using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using ITfoxtec.Identity.Tokens;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcJwtUpLogic<TParty, TClient> : OAuthJwtUpLogic<TParty, TClient> where TParty : OAuthUpParty<TClient> where TClient : OAuthUpClient
    {
        public OidcJwtUpLogic(TelemetryScopedLogger logger, ClientKeySecretLogic<TClient> clientKeySecretLogic, IHttpContextAccessor httpContextAccessor) : base(logger, clientKeySecretLogic, httpContextAccessor)
        { }

        public async Task<ClaimsPrincipal> ValidateIdTokenAsync(string idToken, string issuer, TParty party, string clientId)
        {
            (var validKeys, var invalidKeys) = party.Keys.GetValidKeys();
            try
            {
                (var claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(idToken, issuer, validKeys, clientId, validateIssuer: !issuer.IsNullOrWhiteSpace()));
                return claimsPrincipal;
            }
            catch (Exception ex)
            {
                var ikex = GetInvalidKeyException(invalidKeys, ex);
                if (ikex != null)
                {
                    throw ikex;
                }
                throw;
            }
        }

        private Exception GetInvalidKeyException(IEnumerable<(JsonWebKey key, X509Certificate2 certificate)> invalidKeys, Exception ex)
        {
            if (invalidKeys.Count() > 0)
            {
                var keyInfo = invalidKeys.Select(k => 
                    $"'{(k.certificate != null ? $"{k.certificate.Subject}, Valid from {k.certificate.NotBefore.ToShortDateString()} to {k.certificate.NotAfter.ToShortDateString()}, " : string.Empty)}Key ID: {k.key.Kid}'");
                throw new Exception($"Invalid party keys {string.Join(", ", keyInfo)}.", ex);
            }
            return null;
        }
    }
}
