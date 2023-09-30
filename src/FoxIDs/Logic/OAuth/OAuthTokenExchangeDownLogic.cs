using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
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
        private readonly ITenantRepository tenantRepository;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly OAuthJwtDownLogic<TClient, TScope, TClaim> oauthJwtDownLogic;
        private readonly SecretHashLogic secretHashLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic;

        public OAuthTokenExchangeDownLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ITenantRepository tenantRepository, PlanUsageLogic planUsageLogic, OAuthJwtDownLogic<TClient, TScope, TClaim> oauthJwtDownLogic, SecretHashLogic secretHashLogic, TrackIssuerLogic trackIssuerLogic, ClaimTransformLogic claimTransformLogic, OAuthResourceScopeDownLogic<TClient, TScope, TClaim> oauthResourceScopeDownLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tenantRepository = tenantRepository;
            this.planUsageLogic = planUsageLogic;
            this.oauthJwtDownLogic = oauthJwtDownLogic;
            this.secretHashLogic = secretHashLogic;
            this.trackIssuerLogic = trackIssuerLogic;
            this.claimTransformLogic = claimTransformLogic;
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
            logger.ScopeTrace(() => "Down, OAuth Token Exchange accepted.", triggerEvent: true);

            try
            {
                if (tokenExchangeRequest.SubjectTokenType != IdentityConstants.TokenTypeIdentifiers.AccessToken && tokenExchangeRequest.SubjectTokenType != IdentityConstants.TokenTypeIdentifiers.Saml2)
                {
                    throw new NotSupportedException($"Subject token type not supported. Supported types ['{IdentityConstants.TokenTypeIdentifiers.AccessToken}', '{IdentityConstants.TokenTypeIdentifiers.Saml2}'].");
                }

                //https://cloudentity.com/developers/basics/oauth-extensions/token-exchange/



                var subjectClaims = await ValidateSubjectTokenAsync(party, tokenExchangeRequest.SubjectToken, tokenExchangeRequest.SubjectTokenType);               
                if (subjectClaims?.Count() <= 0)
                {
                    throw new Exception("Subject token not valid.");
                }


                if (party.Client.DisableClientAsTokenExchangeActor)
                {

                }
                else
                {

                }




                var tokenExchangeResponse = new TokenExchangeResponse
                {
                    TokenType = IdentityConstants.TokenTypes.Bearer,
                    ExpiresIn = party.Client.AccessTokenLifetime,
                };

                string algorithm = IdentityConstants.Algorithms.Asymmetric.RS256;

                var claims = new List<Claim>();
                claims.AddClaim(JwtClaimTypes.Subject, $"c_{party.Client.ClientId}");
                claims.AddClaim(JwtClaimTypes.AuthTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                //TODO should the amr claim be included???
                //claims.AddClaim(JwtClaimTypes.Amr, IdentityConstants.AuthenticationMethodReferenceValues.Pwd);

                logger.ScopeTrace(() => $"Down, OAuth created JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                claims = await claimTransformLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
                logger.ScopeTrace(() => $"Down, OAuth output JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
                logger.SetUserScopeProperty(claims);

                var scopes = tokenExchangeRequest.Scope.ToSpaceList();

                tokenExchangeResponse.AccessToken = await oauthJwtDownLogic.CreateAccessTokenAsync(party.Client, claims, scopes, algorithm);

                planUsageLogic.LogTokenRequestEvent(UsageLogTokenTypes.ClientCredentials);

                logger.ScopeTrace(() => $"Token response '{tokenExchangeResponse.ToJsonIndented()}'.", traceType: TraceTypes.Message);
                logger.ScopeTrace(() => "Down, OAuth Token response.", triggerEvent: true);
                return new JsonResult(tokenExchangeResponse);
            }
            catch (KeyException kex)
            {
                throw new OAuthRequestException(kex.Message, kex) { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.ServerError };
            }
        }

        private async Task<IEnumerable<Claim>> ValidateSubjectTokenAsync(TParty party, string subjectToken, string subjectTokenType)
        {
            var subjectSecurityToken = JwtHandler.ReadToken(subjectToken);
            if (subjectTokenType == IdentityConstants.TokenTypeIdentifiers.AccessToken && trackIssuerLogic.GetIssuer().Equals(subjectSecurityToken.Issuer))
            {
                // access token (OIDC/OAuth) issued to the client (audience=client_id) in the same track
                return (await oauthJwtDownLogic.ValidatePartyClientTokenAsync(party.Client, subjectToken))?.Claims;
            }
            else
            {
                var tokenExchangeUpParties = party.AllowUpParties?.Where(up => !up.DisableTokenExchangeTrust);
                if (tokenExchangeUpParties?.Count() <= 0)
                {
                    throw new OAuthRequestException($"Invalid client id '{party.Client.ClientId}', require at least one allowed up-party with token exchange trust.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                }

                var subjectUpParties = tokenExchangeUpParties.Where(tup => tup.Issuers.Any(i => i.Equals(subjectSecurityToken.Issuer, StringComparison.Ordinal)));
                if (subjectUpParties.Count() > 1)
                {
                    throw new OAuthRequestException($"Invalid client id '{party.Client.ClientId}', more then one matching issuer '{subjectSecurityToken.Issuer}' in allowed up-party.") { RouteBinding = RouteBinding, Error = IdentityConstants.ResponseErrors.InvalidClient };
                }

                var subjectUpPartie = subjectUpParties.First();
                if ((subjectUpPartie.Type == PartyTypes.OAuth2 || subjectUpPartie.Type == PartyTypes.Oidc) && subjectTokenType != IdentityConstants.TokenTypeIdentifiers.AccessToken)
                {
                    throw new NotSupportedException($"Subject token type not supported for up-party type {subjectUpPartie.Type}. Supported types ['{IdentityConstants.TokenTypeIdentifiers.AccessToken}'].");
                }
                if ((subjectUpPartie.Type == PartyTypes.Saml2) && subjectTokenType != IdentityConstants.TokenTypeIdentifiers.Saml2)
                {
                    throw new NotSupportedException($"Subject token type not supported for up-party type {subjectUpPartie.Type}. Supported types ['{IdentityConstants.TokenTypeIdentifiers.Saml2}'].");
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
                        throw new NotSupportedException($"Party type '{RouteBinding.UpParty.Type}' not supported.");
                }





                // validate token by AllowTokenUpParties
                //    party.Client.AllowTokenUpParties
                //            - List med
                //                - data id navn...
                //                - typen - OIDC/OAuth eller SAML2
                //                - issuer (ClientID eller custum issuer)
                //            - resolve keys

                //    Ny type up-parties - token up-parties

                // find up-party i listen by subjectToken.Issuer
                // checke om up-party type (OIDC/OAuth eller SAML2) og tokenExchangeRequest.SubjectTokenType passer sammen



            }
        }



        private async Task<IEnumerable<Claim>> ValidateSubjectTokenSamlUpPartyAsync(UpPartyLink subjectUpPartie, string subjectTokenType)
        {
            throw new NotImplementedException();
        }
    }
}
