using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using ITfoxtec.Identity.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OAuthTokenExchangeDownLogic<TParty, TClient, TScope, TClaim> : LogicSequenceBase where TParty : OAuthDownParty<TClient, TScope, TClaim> where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly OAuthJwtDownLogic<TClient, TScope, TClaim> oauthJwtDownLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly ClaimValidationLogic claimValidationLogic;
        private readonly OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic;

        public OAuthTokenExchangeDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, PlanUsageLogic planUsageLogic, OAuthJwtDownLogic<TClient, TScope, TClaim> oauthJwtDownLogic, TrackIssuerLogic trackIssuerLogic, ClaimTransformLogic claimTransformLogic, ClaimValidationLogic claimValidationLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.planUsageLogic = planUsageLogic;
            this.oauthJwtDownLogic = oauthJwtDownLogic;
            this.trackIssuerLogic = trackIssuerLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.claimValidationLogic = claimValidationLogic;
            this.oauthResourceScopeDownLogic = oauthResourceScopeDownLogic;
        }

        public void ValidateTokenExchangeRequest(TClient client, TokenExchangeRequest tokenExchangeRequest)
        {
            if (client.DisableTokenExchangeGrant)
            {
                throw new OAuthRequestException($"Token exchange grant is disabled for client id '{client.ClientId}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.AccessDenied };
            }

            tokenExchangeRequest.Validate();

            if (!tokenExchangeRequest.Scope.IsNullOrEmpty())
            {
                var resourceScopes = oauthResourceScopeDownLogic.GetResourceScopes(client as TClient);
                var invalidScope = tokenExchangeRequest.Scope.ToSpaceList().Where(s => !(resourceScopes.Select(rs => rs).Contains(s) || (client.Scopes != null && client.Scopes.Select(ps => ps.Scope).Contains(s))));
                if (invalidScope.Count() > 0)
                {
                    throw new OAuthRequestException($"Invalid scope '{tokenExchangeRequest.Scope}'.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidScope };
                }
            }
        }

        public virtual async Task<IActionResult> TokenExchangeAsync(TParty party, TokenExchangeRequest tokenExchangeRequest)
        {
            logger.ScopeTrace(() => "AppReg, OAuth Token Exchange accepted.", triggerEvent: true);

            try
            {
                if (tokenExchangeRequest.SubjectTokenType != IdentityConstants.TokenTypeIdentifiers.AccessToken && tokenExchangeRequest.SubjectTokenType != IdentityConstants.TokenTypeIdentifiers.Saml2)
                {
                    throw new NotSupportedException($"Subject token type not supported. Supported types ['{IdentityConstants.TokenTypeIdentifiers.AccessToken}', '{IdentityConstants.TokenTypeIdentifiers.Saml2}'].");
                }

                (var claims, bool sameTrack) = await ValidateSubjectTokenAsync(party, tokenExchangeRequest.SubjectToken, tokenExchangeRequest.SubjectTokenType);               
                if (!(claims?.Count() > 0))
                {
                    throw new OAuthRequestException($"Subject token not valid. Client id '{party.Client.ClientId}'") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.AccessDenied };
                }

                var tokenExchangeResponse = new TokenExchangeResponse
                {
                    IssuedTokenType = IdentityConstants.TokenTypeIdentifiers.AccessToken,
                    TokenType = IdentityConstants.TokenTypes.Bearer,
                    ExpiresIn = party.Client.AccessTokenLifetime,
                };

                string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;

                claims = claims.Where(c => c.Type != JwtClaimTypes.ClientId && c.Type != JwtClaimTypes.Actor && 
                    (!sameTrack || c.Type != Constants.JwtClaimTypes.AuthMethod && c.Type != Constants.JwtClaimTypes.AuthMethodType && c.Type != Constants.JwtClaimTypes.UpParty && c.Type != Constants.JwtClaimTypes.UpPartyType)).ToList();
                logger.ScopeTrace(() => $"AppReg, OAuth received JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                if (!party.Client.DisableClientAsTokenExchangeActor)
                {
                    claims.AddClaim(JwtClaimTypes.Actor, GetPartyActorClaims(party).ToJson());
                }
                var transformedClaims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                logger.ScopeTrace(() => $"AppReg, OAuth output JWT claims '{transformedClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                logger.SetUserScopeProperty(transformedClaims);

                var scopes = tokenExchangeRequest.Scope.ToSpaceList();
                tokenExchangeResponse.AccessToken = await oauthJwtDownLogic.CreateAccessTokenAsync(party.Client, transformedClaims, scopes, algorithm);

                planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.TokenExchange);

                logger.ScopeTrace(() => $"Token response '{tokenExchangeResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                logger.ScopeTrace(() => "AppReg, OAuth Token response.", triggerEvent: true);
                return new JsonResult(tokenExchangeResponse);
            }
            catch (KeyException kex)
            {
                throw new OAuthRequestException(kex.Message, kex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.ServerError };
            }
        }

        private Dictionary<string, string> GetPartyActorClaims(TParty party)
        {
            var claims = new Dictionary<string, string>
            {
                { JwtClaimTypes.Subject, $"c_{party.Client.ClientId}" }
            };
            return claims;
        }

        private async Task<(List<Claim> claims, bool sameTrack)> ValidateSubjectTokenAsync(TParty party, string subjectToken, string subjectTokenType)
        {
            var trackIssuer = trackIssuerLogic.GetIssuer();
            (string subjectTokenIssuer, IEnumerable<string> subjectTokenAudiences) = ReadSubjectTokenIssuerAndAudiences(subjectToken, subjectTokenType);

            var claims = await ValidateSubjectTokenByUpPartyAsync(party, trackIssuer, subjectToken, subjectTokenType, subjectTokenIssuer, subjectTokenAudiences);
            if (claims != null)
            {
                return (claims, false);
            }
            if (subjectTokenType == IdentityConstants.TokenTypeIdentifiers.AccessToken && subjectTokenIssuer.Equals(trackIssuer, StringComparison.Ordinal))
            {
                claims = await ValidateSameTrackSubjectTokenAsync(party, subjectToken);
                if (claims == null)
                {
                    throw new OAuthRequestException($"Subject token not accepted in the same environment, the claims collection is empty. Client id '{party.Client.ClientId}'") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.AccessDenied };
                }
                return (claims, true);
            }
            else
            {
                throw new OAuthRequestException($"Require at least one allowed authentication method with matching issuer '{subjectTokenIssuer}' and a audience '{string.Join(", ", subjectTokenAudiences)}'. Client id '{party.Client.ClientId}'") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
            }
        }

        private (string issuer, IEnumerable<string> audiences) ReadSubjectTokenIssuerAndAudiences(string subjectToken, string subjectTokenType)
        {
            switch (subjectTokenType)
            {
                case IdentityConstants.TokenTypeIdentifiers.AccessToken:
                    var subjectSecurityToken = JwtHandler.ReadToken(subjectToken);
                    return (subjectSecurityToken.Issuer, subjectSecurityToken.Audiences);
                case IdentityConstants.TokenTypeIdentifiers.Saml2:
                    return SamlAuthnUpLogic.ReadTokenExchangeSubjectTokenIssuerAndAudiencesAsync(subjectToken);
                default:
                    throw new NotSupportedException($"Subject token type '{subjectTokenType}' not supported.");
            }
        }

        private async Task<List<Claim>> ValidateSubjectTokenByUpPartyAsync(TParty party, string trackIssuer, string subjectToken, string subjectTokenType, string subjectTokenIssuer, IEnumerable<string> subjectTokenAudiences)
        {
            var tokenExchangeUpParties = party.AllowUpParties?.Where(up => (up.Type == PartyTypes.OAuth2 || up.Type == PartyTypes.Oidc || up.Type == PartyTypes.Saml2) && !up.DisableTokenExchangeTrust);
            if (!(tokenExchangeUpParties?.Count() > 0))
            {
                return null;
            }
            
            var subjectUpParties = tokenExchangeUpParties.Where(tup => tup.Issuers.Any(i => i.Equals(subjectTokenIssuer, StringComparison.Ordinal)) && 
                subjectTokenAudiences.Any(a => !string.IsNullOrEmpty(tup.SpIssuer) ? tup.SpIssuer.Equals(a, StringComparison.Ordinal) : trackIssuer.Equals(a, StringComparison.Ordinal)));

            if (!(subjectUpParties?.Count() > 0))
            {
                return null;
            }
            if (subjectUpParties.Count() > 1)
            {
                throw new OAuthRequestException($"More then one matching issuer '{subjectTokenIssuer}' and a audience '{string.Join(", ", subjectTokenAudiences)}' in allowed authentication method. Client id '{party.Client.ClientId}'") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
            }

            var subjectUpPartie = subjectUpParties.First();
            if ((subjectUpPartie.Type == PartyTypes.OAuth2 || subjectUpPartie.Type == PartyTypes.Oidc) && subjectTokenType != IdentityConstants.TokenTypeIdentifiers.AccessToken)
            {
                throw new NotSupportedException($"Subject token type not supported for authentication method type {subjectUpPartie.Type}. Supported types ['{IdentityConstants.TokenTypeIdentifiers.AccessToken}'].");
            }
            if ((subjectUpPartie.Type == PartyTypes.Saml2) && subjectTokenType != IdentityConstants.TokenTypeIdentifiers.Saml2)
            {
                throw new NotSupportedException($"Subject token type not supported for authentication method type {subjectUpPartie.Type}. Supported types ['{IdentityConstants.TokenTypeIdentifiers.Saml2}'].");
            }

            switch (subjectUpPartie.Type)
            {
                case PartyTypes.OAuth2:
                    return await serviceProvider.GetService<OAuthAuthUpLogic<OAuthUpParty, OAuthUpClient>>().ValidateTokenExchangeSubjectTokenAsync(subjectUpPartie, subjectToken);
                case PartyTypes.Oidc:
                    return await serviceProvider.GetService<OAuthAuthUpLogic<OidcUpParty, OidcUpClient>>().ValidateTokenExchangeSubjectTokenAsync(subjectUpPartie, subjectToken);
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlAuthnUpLogic>().ValidateTokenExchangeSubjectTokenAsync(subjectUpPartie, subjectToken);
                default:
                    throw new NotSupportedException($"Connection type '{RouteBinding.UpParty.Type}' not supported.");
            }
        }

        private async Task<List<Claim>> ValidateSameTrackSubjectTokenAsync(TParty party, string subjectToken)
        {
            logger.ScopeTrace(() => "AppReg, OAuth validate same environment token exchange subject token.");

            var claimsPrincipal = await oauthJwtDownLogic.ValidateTokenAsync(subjectToken, audience: party.Name);
            if (claimsPrincipal == null)
            {
                throw new OAuthRequestException($"Subject token not accepted in the same environment, perhaps due to incorrect audience. Client id (required audience) '{party.Client.ClientId}'") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.AccessDenied };
            }

            var claims = claimsPrincipal.Claims?.ToList();
            logger.ScopeTrace(() => "AppReg, OAuth same environment subject token valid.", triggerEvent: true);
            logger.ScopeTrace(() => $"AppReg, OAuth same environment received JWT claims '{claims}'", traceType: TraceTypes.Claim);

            var validClaims = claimValidationLogic.ValidateUpPartyClaims(new List<string> { "*" }, claims);

            logger.ScopeTrace(() => $"AuthMethod, OAuth same environment output JWT claims '{validClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return validClaims;
        }
    }
}
