using FoxIDs.Infrastructure;
using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Logic;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System;
using FoxIDs.Repository;
using FoxIDs.Models;
using System.Linq;
using FoxIDs.Models.Config;
using ITfoxtec.Identity.Util;
using ITfoxtec.Identity.Messages;
using System.Collections.Generic;
using FoxIDs.Client.Util;
using static ITfoxtec.Identity.IdentityConstants;
using Microsoft.AspNetCore.DataProtection;
using System.Net.Http;
using System.Net;
using System.Security.Claims;
using ITfoxtec.Identity.Tokens;
using AutoMapper;

namespace FoxIDs.Controllers
{
    public class TDownPartyTestController : ApiController
    {        
        private const string testPartyV2Key = "v2";

        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IMapper mapper;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly PartyLogic partyLogic;
        private readonly IDataProtectionProvider dataProtection;
        private readonly ValidateModelGenericPartyLogic validateModelGenericPartyLogic;
        private readonly SecretHashLogic secretHashLogic;
        private readonly OidcDiscoveryReadLogic oidcDiscoveryReadLogic;
        private readonly IHttpClientFactory httpClientFactory;

        public TDownPartyTestController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic, IDataProtectionProvider dataProtection, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, SecretHashLogic secretHashLogic, OidcDiscoveryReadLogic oidcDiscoveryReadLogic, IHttpClientFactory httpClientFactory) : base(logger, auditLogEnabled: false)
        {
            this.settings = settings;
            this.logger = logger;
            this.mapper = mapper;
            this.tenantDataRepository = tenantDataRepository;
            this.partyLogic = partyLogic;
            this.dataProtection = dataProtection;
            this.validateModelGenericPartyLogic = validateModelGenericPartyLogic;
            this.secretHashLogic = secretHashLogic;
            this.oidcDiscoveryReadLogic = oidcDiscoveryReadLogic;
            this.httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Start new down-party test.
        /// </summary>
        /// <param name="testDownPartyRequest">Down-party test start request.</param>
        /// <returns>Down-party test.</returns>
        [ProducesResponseType(typeof(Api.DownPartyTestStartResponse), StatusCodes.Status200OK)]
        [TenantScopeAuthorize(Constants.ControlApi.Segment.Basic, Constants.ControlApi.Segment.Party)]
        public async Task<ActionResult<Api.DownPartyTestStartResponse>> PostDownPartyTest([FromBody] Api.DownPartyTestStartRequest testDownPartyRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(testDownPartyRequest)) return BadRequest(ModelState);

            await partyLogic.DeleteExporedDownParties();

            var partyName = await partyLogic.GeneratePartyNameAsync(false);

            try
            {
                var secret = SecretGenerator.GenerateNewSecret();
                var codeVerifier = RandomGenerator.Generate(64);

                var authenticationRequest = new AuthenticationRequest
                {
                    ClientId = partyName,
                    ResponseMode = testDownPartyRequest.ResponseMode,
                    ResponseType = ResponseTypes.Code,
                    RedirectUri = testDownPartyRequest.RedirectUri,
                    Scope = new[] { DefaultOidcScopes.OpenId, DefaultOidcScopes.Profile, DefaultOidcScopes.Email, DefaultOidcScopes.Address, DefaultOidcScopes.Phone }.ToSpaceList(),
                    Nonce = RandomGenerator.GenerateNonce(),
                    State = $"{RouteBinding.TrackName}{Constants.Models.OidcDownPartyTest.StateSplitKey}{partyName}{Constants.Models.OidcDownPartyTest.StateSplitKey}{testPartyV2Key}{Constants.Models.OidcDownPartyTest.StateSplitKey}{CreateProtector(RouteBinding.TrackName).Protect(secret)}"
                };

                var codeChallengeRequest = new CodeChallengeSecret
                {
                    CodeChallenge = await codeVerifier.Sha256HashBase64urlEncodedAsync(),
                    CodeChallengeMethod = CodeChallengeMethods.S256,
                };

                var requestDictionary = authenticationRequest.ToDictionary().AddToDictionary(codeChallengeRequest);
                var testUrl = QueryHelpers.AddQueryString(UrlCombine.Combine(GetAuthority(partyName), Constants.Routes.OAuthController, Constants.Endpoints.Authorize), requestDictionary);

                var mParty = new OidcDownParty
                {
                    Id = await DownParty.IdFormatAsync(RouteBinding, partyName),
                    Name = partyName,
                    IsTest = true,
                    TestUrl = testUrl,
                    TestExpireAt = DateTimeOffset.UtcNow.AddSeconds(settings.DownPartyTestLifetime).ToUnixTimeSeconds(),
                    TestExpireInSeconds = settings.DownPartyTestLifetime,
                    Nonce = authenticationRequest.Nonce,
                    CodeVerifier = codeVerifier,                    
                    AllowUpParties = testDownPartyRequest.UpParties.Select(p => new UpPartyLink { Name = p.Name.ToLower(), ProfileName = p.ProfileName?.ToLower()  }).ToList(),
                    Client = new OidcDownClient
                    {
                        RedirectUris = new List<string> { authenticationRequest.RedirectUri },
                        ResponseTypes = new List<string> { authenticationRequest.ResponseType },
                        ClientAuthenticationMethod = Models.ClientAuthenticationMethods.ClientSecretPost,
                        RequirePkce = true,
                        Claims = testDownPartyRequest.Claims.Select(c => new OidcDownClaim { Claim = c }).ToList(),
                        ResourceScopes = new List<OAuthDownResourceScope> { new OAuthDownResourceScope { Resource = partyName } },
                        Scopes = new List<OidcDownScope>
                        {
                            new OidcDownScope { Scope = DefaultOidcScopes.OfflineAccess },
                            new OidcDownScope
                            {
                                Scope = DefaultOidcScopes.Profile,
                                VoluntaryClaims = new List<OidcDownClaim>
                                {
                                    new OidcDownClaim { Claim = JwtClaimTypes.Name, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.GivenName, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.MiddleName, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.FamilyName, InIdToken = true },
                                    new OidcDownClaim { Claim = JwtClaimTypes.Nickname, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.PreferredUsername, InIdToken = true },
                                    new OidcDownClaim { Claim = JwtClaimTypes.Birthdate, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Gender, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Picture, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Profile, InIdToken = false },
                                    new OidcDownClaim { Claim = JwtClaimTypes.Website, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.Locale, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.Zoneinfo, InIdToken = false }, new OidcDownClaim { Claim = JwtClaimTypes.UpdatedAt, InIdToken = false }
                                }
                            },
                            new OidcDownScope { Scope = DefaultOidcScopes.Email, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.Email, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.EmailVerified, InIdToken = false } } },
                            new OidcDownScope { Scope = DefaultOidcScopes.Address, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.Address, InIdToken = true } } },
                            new OidcDownScope { Scope = DefaultOidcScopes.Phone, VoluntaryClaims = new List<OidcDownClaim> { new OidcDownClaim { Claim = JwtClaimTypes.PhoneNumber, InIdToken = true }, new OidcDownClaim { Claim = JwtClaimTypes.PhoneNumberVerified, InIdToken = false } } }
                        },
                        DisableClientCredentialsGrant = true,
                        DisableTokenExchangeGrant = true,                        
                    },
                };

                var oauthClientSecret = new OAuthClientSecret();
                await secretHashLogic.AddSecretHashAsync(oauthClientSecret, secret);
                mParty.Client.Secrets = [oauthClientSecret];

                if (!await validateModelGenericPartyLogic.ValidateModelAllowUpPartiesAsync(ModelState, nameof(testDownPartyRequest.UpParties), mParty)) return BadRequest(ModelState);

                mParty.DisplayName = $"Test application {(mParty.AllowUpParties.Count() == 1 ? $"[{GetUpPartyDisplayName(mParty.AllowUpParties.First())}]" : $"- {mParty.Name}")}";

                await tenantDataRepository.CreateAsync(mParty);

                return Ok(new Api.DownPartyTestStartResponse
                {
                    Name = mParty.Name,
                    DisplayName = mParty.DisplayName,
                    TestUrl = testUrl,
                    TestExpireAt = mParty.TestExpireAt.Value,
                    TestExpireInSeconds = mParty.TestExpireInSeconds.Value,
                });
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.Conflict)
                {
                    logger.Warning(ex, $"Conflict, Create '{typeof(OidcDownParty).Name}' by name '{partyName}'.");
                    return Conflict(typeof(OidcDownParty).Name, partyName, nameof(OidcDownParty.Name));
                }
                throw;
            }
        }

        private string GetUpPartyDisplayName(UpPartyLink upParty)
        {
            if (upParty.Type == PartyTypes.Login)
            {
                return $"{upParty.DisplayName ?? (upParty.Name == Constants.DefaultLogin.Name ? "Default" : upParty.Name)} (User login UI)";
            }
            else if (upParty.Type == PartyTypes.OAuth2)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (OAuth 2.0)";
            }
            else if (upParty.Type == PartyTypes.Oidc)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (OpenID Connect)";
            }
            else if (upParty.Type == PartyTypes.Saml2)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (SAML 2.0)";
            }
            else if (upParty.Type == PartyTypes.TrackLink)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (Environment Link)";
            }
            else if (upParty.Type == PartyTypes.ExternalLogin)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (External Login)";
            }
            throw new NotSupportedException($"Type '{upParty.Type}'.");
        }

        /// <summary>
        /// Get the down-party test result.
        /// </summary>
        /// <param name="testDownPartyRequest">Down-party test result request.</param>
        /// <returns>Down-party test.</returns>
        [ProducesResponseType(typeof(Api.DownPartyTestResultResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.DownPartyTestResultResponse>> PutDownPartyTest([FromBody] Api.DownPartyTestResultRequest testDownPartyRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(testDownPartyRequest)) return BadRequest(ModelState);

            var stateSplit = testDownPartyRequest.State.Split(Constants.Models.OidcDownPartyTest.StateSplitKey);
            if (!(stateSplit.Length >= 3))
            {
                throw new Exception("Invalid state format.");
            }
            var trackName = stateSplit[0];
            if (!RouteBinding.TrackName.Equals(trackName, StringComparison.Ordinal))
            {
                throw new Exception("Invalid state track.");
            }

            var partyName = stateSplit[1];
            await partyLogic.DeleteExporedDownParties();

            try
            {
                var mParty = await tenantDataRepository.GetAsync<OidcDownParty>(await DownParty.IdFormatAsync(RouteBinding, partyName));

                var clientSecret = stateSplit[2] == testPartyV2Key ? CreateProtector(RouteBinding.TrackName).Unprotect(stateSplit[3]) : CreateProtector(mParty.Name).Unprotect(stateSplit[2]);
                (var tokenResponse, var idTokenPrincipal, var accessTokenPrincipal) = await AcquireTokensAsync(mParty, clientSecret, mParty.Nonce, testDownPartyRequest.Code);

                var rpInitiatedLogoutRequest = new RpInitiatedLogoutRequest
                {
                    IdTokenHint = tokenResponse.IdToken,
                    PostLogoutRedirectUri = mParty.Client.RedirectUris.First(),
                };
                var requestDictionary = rpInitiatedLogoutRequest.ToDictionary();
                var endSessionUrl = QueryHelpers.AddQueryString(UrlCombine.Combine(GetAuthority(mParty.Name), Constants.Routes.OAuthController, Constants.Endpoints.EndSession), requestDictionary);

                var testUpPartyResultResponse = new Api.DownPartyTestResultResponse
                {
                    Name = mParty.Name,
                    DisplayName = mParty.DisplayName,
                    IdTokenClaims = mapper.Map<List<Api.ClaimAndValues>>(idTokenPrincipal.Claims.ToClaimAndValues()),
                    AccessTokenClaims = mapper.Map<List<Api.ClaimAndValues>>(accessTokenPrincipal.Claims.ToClaimAndValues()),
                    IdToken = tokenResponse.IdToken,
                    AccessToken = tokenResponse.AccessToken,
                    EndSessionUrl = endSessionUrl,
                    TestUrl = mParty.TestUrl,
                    TestExpireAt = mParty.TestExpireAt ?? 0,
                    TestExpireInSeconds = mParty.TestExpireInSeconds ?? 0,
                };
                return Ok(testUpPartyResultResponse);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (ResponseErrorException rex)
            {
                logger.Warning(rex, $"Response error, Update '{typeof(OidcDownParty).Name}' by name '{partyName}'.");
                ModelState.AddModelError(string.Empty, rex.Message);
                return BadRequest(ModelState);
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(OidcDownParty).Name}' by name '{partyName}'.");
                    return NotFound("Test application was not found, it has probably expired.");
                }
                throw;
            }
        }
        private string GetAuthority(string partyName)
        {
            var routeBinding = RouteBinding;
            var useValidCustomDomain = !routeBinding.TrackName.Equals(Constants.Routes.MasterTrackName, StringComparison.OrdinalIgnoreCase) && routeBinding.HasVerifiedCustomDomain;

            var urlItems = new List<string>();
            if (!useValidCustomDomain)
            {
                urlItems.Add(routeBinding.TenantName);
            }
            urlItems.Add(routeBinding.TrackName);
            urlItems.Add($"{partyName}(*)");

            return UrlCombine.Combine(useValidCustomDomain ? $"{HttpContext.Request.Scheme}://{routeBinding.CustomDomain}" : settings.FoxIDsEndpoint, urlItems.ToArray());
        }

        private async Task<(TokenResponse tokenResponse, ClaimsPrincipal idTokenPrincipal, ClaimsPrincipal accessTokenPrincipal)> AcquireTokensAsync(OidcDownParty mParty, string clientSecret, string nonce, string code)
        {
            var tokenRequest = new TokenRequest
            {
                GrantType = GrantTypes.AuthorizationCode,
                Code = code,
                ClientId = mParty.Name,
                RedirectUri = mParty.Client.RedirectUris.First(),
            };

            var clientCredentials = new ClientCredentials
            {
                ClientSecret = clientSecret,
            };

            var codeVerifierSecret = new CodeVerifierSecret
            {
                CodeVerifier = mParty.CodeVerifier,
            };

            (var oidcDiscovery, var jsonWebKeySet) = await oidcDiscoveryReadLogic.GetOidcDiscoveryAndValidateAsync(GetAuthority(mParty.Name));

            var requestDictionary = tokenRequest.ToDictionary().AddToDictionary(clientCredentials).AddToDictionary(codeVerifierSecret);

            var request = new HttpRequestMessage(HttpMethod.Post, oidcDiscovery.TokenEndpoint);
            request.Content = new FormUrlEncodedContent(requestDictionary);

            var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var tokenResponse = result.ToObject<TokenResponse>();
                    tokenResponse.Validate(true);
                    if (tokenResponse.AccessToken.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tokenResponse.AccessToken), tokenResponse.GetTypeName());
                    if (tokenResponse.ExpiresIn <= 0) throw new ArgumentNullException(nameof(tokenResponse.ExpiresIn), tokenResponse.GetTypeName());

                    (var idTokenPrincipal, _) = JwtHandler.ValidateToken(tokenResponse.IdToken, oidcDiscovery.Issuer, jsonWebKeySet.Keys, mParty.Name);
                    var nonceClaimValue = idTokenPrincipal.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Nonce);
                    if (!nonce.Equals(nonceClaimValue, StringComparison.Ordinal))
                    {
                        throw new Exception("Id token nonce do not match.");
                    }

                    var atHash = idTokenPrincipal.Claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.AtHash);
                    string algorithm = Algorithms.Asymmetric.RS256;
                    if (atHash != await tokenResponse.AccessToken.LeftMostBase64urlEncodedHashAsync(algorithm))
                    {
                        throw new Exception("Access Token hash claim in ID token do not match the access token.");
                    }

                    return (tokenResponse, idTokenPrincipal, JwtHandler.ReadTokenClaims(tokenResponse.AccessToken));

                default:
                    var resultBadRequest = await response.Content.ReadAsStringAsync();
                    var tokenResponseBadRequest = resultBadRequest.ToObject<TokenResponse>();
                    tokenResponseBadRequest.Validate(true);
                    throw new Exception($"Error login call back, unexpected status code. StatusCode={response.StatusCode}");
            }
        }

        private IDataProtector CreateProtector(string partyName)
        {
            return dataProtection.CreateProtector([RouteBinding.TenantName, RouteBinding.TrackName, partyName]);
        }
    }
}
