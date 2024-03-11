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
    public class OAuthJwtUpLogic<TParty, TClient> : LogicSequenceBase where TParty : OAuthUpParty<TClient> where TClient : OAuthUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ClientKeySecretLogic<TClient> clientKeySecretLogic;

        public OAuthJwtUpLogic(TelemetryScopedLogger logger, ClientKeySecretLogic<TClient> clientKeySecretLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.clientKeySecretLogic = clientKeySecretLogic;
        }

        public async Task<ClaimsPrincipal> ValidateAccessTokenAsync(string accessToken, string issuer, TParty party, string audience = null)
        {
            (var validKeys, var invalidKeys) = party.Keys.GetValidKeys();
            try
            {
                (var claimsPrincipal, _) = await Task.FromResult(JwtHandler.ValidateToken(accessToken, issuer, validKeys, audience: audience, validateIssuer: !issuer.IsNullOrWhiteSpace(), validateAudience: !audience.IsNullOrWhiteSpace()));
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

        public async Task<string> CreateClientAssertionAsync(TClient client, string clientId, string algorithm)
        {
            var clientAssertionClaims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Issuer, clientId),
                new Claim(JwtClaimTypes.Subject, clientId),
                new Claim(JwtClaimTypes.Audience, client.TokenUrl),
                new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString())
            };

            logger.ScopeTrace(() => $"AuthMethod, JWT client assertion claims '{clientAssertionClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            var claims = clientAssertionClaims.Where(c => c.Type != JwtClaimTypes.Issuer && c.Type != JwtClaimTypes.Audience);
            var token = JwtHandler.CreateToken(clientKeySecretLogic.GetClientKey(client), clientAssertionClaims.Single(c => c.Type == JwtClaimTypes.Issuer).Value, clientAssertionClaims.Single(c => c.Type == JwtClaimTypes.Audience).Value, 
                claims, expiresIn: client.ClientAssertionLifetime, algorithm: algorithm);
            return await token.ToJwtString();
        }
    }
}
