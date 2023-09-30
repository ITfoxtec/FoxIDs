using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using ITfoxtec.Identity.Tokens;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OAuthAuthUpLogic<TParty, TClient> : LogicSequenceBase where TParty : OAuthUpParty<TClient> where TClient : OAuthUpClient
    {
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly OAuthJwtUpLogic<TParty, TClient> oauthJwtUpLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ClaimValidationLogic claimValidationLogic;
        private readonly IHttpClientFactory httpClientFactory;

        public OAuthAuthUpLogic(TelemetryScopedLogger logger, ITenantRepository tenantRepository, OAuthJwtUpLogic<TParty, TClient> oauthJwtUpLogic, ClaimTransformLogic claimTransformLogic, ClaimValidationLogic claimValidationLogic, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.oauthJwtUpLogic = oauthJwtUpLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.claimValidationLogic = claimValidationLogic;
            this.httpClientFactory = httpClientFactory;
        }

        protected async Task<List<Claim>> ValidateTokenAsync(TParty party, string accessToken)
        {
            List<Claim> claims = null;
            if (party.Client.UseUserInfoClaims)
            {
                claims = await UserInforRequestAsync(party.Client, accessToken);
            }
            else
            {
                var sessionIdClaim = claims.Where(c => c.Type == JwtClaimTypes.SessionId).FirstOrDefault();
                claims = await ValidateAccessTokenAsync(party, ResolveClientId(party), accessToken);
                if (sessionIdClaim != null && !claims.Where(c => c.Type == JwtClaimTypes.SessionId).Any())
                {
                    claims.Add(sessionIdClaim);
                }
            }

            var accessTokenClaims = claims.Where(c => c.Type == Constants.JwtClaimTypes.AccessToken).Select(c => c.Value);
            if (accessTokenClaims.Count() > 0)
            {
                claims = claims.Where(c => c.Type != Constants.JwtClaimTypes.AccessToken).ToList();
                foreach (var accessTokenClaim in accessTokenClaims)
                {
                    claims.Add(new Claim(Constants.JwtClaimTypes.AccessToken, $"{party.Name}|{accessTokenClaim}"));
                }
            }

            claims.AddClaim(Constants.JwtClaimTypes.AccessToken, $"{party.Name}|{accessToken}");

            var subject = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject);

            if (subject.IsNullOrEmpty())
            {
                subject = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Email);
            }

            if (!subject.IsNullOrEmpty())
            {
                claims = claims.Where(c => c.Type != JwtClaimTypes.Subject).ToList();
                claims.Add(new Claim(JwtClaimTypes.Subject, $"{party.Name}|{subject}"));
            }

            return claims;
        }

        protected virtual async Task<List<Claim>> ValidateAccessTokenAsync(TParty party, string resolvedClientId, string accessToken)
        {
            try
            {
                var jwtToken = JwtHandler.ReadToken(accessToken);
                var issuer = party.Issuers.Where(i => i == jwtToken.Issuer).FirstOrDefault();
                if (string.IsNullOrEmpty(issuer))
                {
                    throw new OAuthRequestException($"{party.Name}|Access token issuer '{jwtToken.Issuer}' is unknown.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
                }

                var claimsPrincipal = await oauthJwtUpLogic.ValidateAccessTokenAsync(accessToken, issuer, party, resolvedClientId);
                return claimsPrincipal.Claims.ToList();
            }
            catch (OAuthRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new OAuthRequestException($"{party.Name}|Access token not valid.", ex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidToken };
            }
        }

        protected virtual async Task<List<Claim>> UserInforRequestAsync(TClient client, string accessToken)
        {
            ValidateClientUserInfoSupport(client);
            logger.ScopeTrace(() => $"Up, OIDC UserInfo request URL '{client.UserInfoUrl}'.", traceType: TraceTypes.Message);

            var httpClient = httpClientFactory.CreateClient(nameof(HttpClient));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);

            using var response = await httpClient.GetAsync(client.UserInfoUrl);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var userInfoResponse = result.ToObject<Dictionary<string, string>>();
                    logger.ScopeTrace(() => $"Up, UserInfo response '{userInfoResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);

                    var claims = userInfoResponse.Select(c => new Claim(c.Key, c.Value)).ToList();
                    if (!claims.Any(c => c.Type == JwtClaimTypes.Subject))
                    {
                        throw new OAuthRequestException($"Require {JwtClaimTypes.Subject} claim from userinfo endpoint.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                    }
                    return claims;

                case HttpStatusCode.BadRequest:
                    var resultBadRequest = await response.Content.ReadAsStringAsync();
                    var userInfoResponseBadRequest = resultBadRequest.ToObject<TokenResponse>();
                    logger.ScopeTrace(() => $"Up, Bad userinfo response '{userInfoResponseBadRequest.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                    try
                    {
                        userInfoResponseBadRequest.Validate(true);
                    }
                    catch (ResponseErrorException rex)
                    {
                        throw new OAuthRequestException($"External {rex.Message}") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                    }
                    throw new EndpointException($"Bad request. Status code '{response.StatusCode}'. Response '{resultBadRequest}'.") { RouteBinding = RouteBinding };

                default:
                    try
                    {
                        var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                        var userInfoResponseUnexpectedStatus = resultUnexpectedStatus.ToObject<TokenResponse>();
                        logger.ScopeTrace(() => $"Up, Unexpected status code userinfo response '{userInfoResponseUnexpectedStatus.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                        try
                        {
                            userInfoResponseUnexpectedStatus.Validate(true);
                        }
                        catch (ResponseErrorException rex)
                        {
                            throw new OAuthRequestException($"External {rex.Message}") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidRequest };
                        }
                    }
                    catch (OAuthRequestException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new EndpointException($"Unexpected status code. Status code={response.StatusCode}", ex) { RouteBinding = RouteBinding };
                    }
                    throw new EndpointException($"Unexpected status code. Status code={response.StatusCode}") { RouteBinding = RouteBinding };
            }
        }

        private void ValidateClientUserInfoSupport(TClient client)
        {
            if (client.UserInfoUrl.IsNullOrEmpty())
            {
                throw new EndpointException("Userinfo endpoint not configured.") { RouteBinding = RouteBinding };
            }
        }

        public async Task<IEnumerable<Claim>> ValidateTokenExchangeSubjectTokenAsync(UpPartyLink partyLink, string subjectToken)
        {
            logger.ScopeTrace(() => "Up, OAuth validate token exchange subject token.");
            var partyId = await UpParty.IdFormatAsync(RouteBinding, partyLink.Name);
            logger.SetScopeProperty(Constants.Logs.UpPartyId, partyId);

            var party = await tenantRepository.GetAsync<TParty>(partyId);

            var claims = await ValidateTokenAsync(party, subjectToken);

            logger.ScopeTrace(() => "Up, OAuth token exchange subject token valid.", triggerEvent: true);
            logger.ScopeTrace(() => $"Up, OAuth received JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            claims = claims.Where(c => c.Type != Constants.JwtClaimTypes.UpParty && c.Type != Constants.JwtClaimTypes.UpPartyType).ToList();
            claims.AddClaim(Constants.JwtClaimTypes.UpParty, party.Name);
            claims.AddClaim(Constants.JwtClaimTypes.UpPartyType, party.Type.ToString().ToLower());

            var transformedClaims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            var validClaims = claimValidationLogic.ValidateUpPartyClaims(party.Client.Claims, transformedClaims);

            logger.ScopeTrace(() => $"Up, OAuth output JWT claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return claims;
        }

        protected string ResolveClientId(TParty party)
        {
            return !party.Client.SpClientId.IsNullOrWhiteSpace() ? party.Client.SpClientId : party.Client.ClientId;
        }
    }
}
