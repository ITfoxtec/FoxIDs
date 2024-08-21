using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Ext = FoxIDs.Models.ExternalLogin;

namespace FoxIDs.Logic
{
    public class ExternalLoginConnectLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly FailingLoginLogic failingLoginLogic;

        public ExternalLoginConnectLogic(TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, FailingLoginLogic failingLoginLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.failingLoginLogic = failingLoginLogic;
        }

        public async Task<List<Claim>> ValidateUserAsync(ExternalLoginUpParty party, string username, string password)
        {
            var claims = party.ExternalLoginType switch
            {
                ExternalLoginTypes.Api => await ValidateUserApiAsync(party, username, password),
                _ => throw new NotSupportedException()
            };

            claims = claims ?? new List<Claim>();           
            logger.ScopeTrace(() => $"AuthMethod, External login, received JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            if (!claims.Any(c => c.Type == JwtClaimTypes.Subject))
            {
                claims.AddClaim(JwtClaimTypes.Subject, username);
            }
            if (party.UsernameType == ExternalLoginUsernameTypes.Email && !claims.Any(c => c.Type == JwtClaimTypes.Email))
            {
                claims.AddClaim(JwtClaimTypes.Email, username);
            }
            return claims;
        }

        private async Task<List<Claim>> ValidateUserApiAsync(ExternalLoginUpParty extLoginUpParty, string username, string password)
        {
            var authenticationApiUrl = UrlCombine.Combine(extLoginUpParty.ApiUrl, Constants.ExternalLogin.Api.Authentication);
            logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API request, URL '{authenticationApiUrl}'.", traceType: TraceTypes.Message);

            var authRequest = new Ext.AuthenticationRequest
            {
                UsernameType = extLoginUpParty.UsernameType,
                Username = username,
                Password = password
            };
            logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API request '{authRequest.ToJson()}'.", traceType: TraceTypes.Message);

            var httpClient = httpClientFactory.CreateClient();
            logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API secret '{(extLoginUpParty.Secret?.Length > 10 ? extLoginUpParty.Secret.Substring(0, 3) : string.Empty)}'.", traceType: TraceTypes.Message);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BasicAuthentication.Basic, $"{Constants.ExternalLogin.Api.ApiId.OAuthUrlDencode()}:{extLoginUpParty.Secret.OAuthUrlDencode()}".Base64Encode());

            var failingLoginCount = await failingLoginLogic.VerifyFailingLoginCountAsync(username, isExternalLogin: true);

            using var response = await httpClient.PostAsJsonAsync(authenticationApiUrl, authRequest);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var authenticationResponse = result.ToObject<Ext.AuthenticationResponse>();
                    logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API response '{authenticationResponse.ToJson()}'.", traceType: TraceTypes.Message);

                    await failingLoginLogic.ResetFailingLoginCountAsync(username, isExternalLogin: true);
                    logger.ScopeTrace(() => $"AuthMethod, External login, User '{username}' and password valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);

                    return authenticationResponse.Claims?.Select(c => new Claim(c.Type, c.Value))?.ToList();

                case HttpStatusCode.Forbidden:
                    logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API response, Status code={response.StatusCode}. Response '{response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}'.", traceType: TraceTypes.Message);

                    throw new InvalidUsernameOrPasswordException($"Username or password invalid, user '{username}'.");

                default:
                    var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                    throw new Exception($"AuthMethod, External login, Authentication API response, Status code={response.StatusCode}. Response '{resultUnexpectedStatus}'.");
            }
        }
    }
}
