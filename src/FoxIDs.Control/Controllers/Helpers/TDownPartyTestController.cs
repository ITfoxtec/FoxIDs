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
        private const char stateSplitKey = ':';
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

        public TDownPartyTestController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IMapper mapper, ITenantDataRepository tenantDataRepository, PartyLogic partyLogic, IDataProtectionProvider dataProtection, ValidateModelGenericPartyLogic validateModelGenericPartyLogic, SecretHashLogic secretHashLogic, OidcDiscoveryReadLogic oidcDiscoveryReadLogic, IHttpClientFactory httpClientFactory) : base(logger)
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
        /// <param name="TestUpPartyStartRequest">Down-party test start request.</param>
        /// <returns>Down-party test.</returns>
        [ProducesResponseType(typeof(Api.DownPartyTestStartResponse), StatusCodes.Status200OK)]
        [TenantScopeAuthorize(Constants.ControlApi.Segment.Base, Constants.ControlApi.Segment.Party)]
        public async Task<ActionResult<Api.DownPartyTestStartResponse>> PostDownPartyTest([FromBody] Api.DownPartyTestStartRequest testUpPartyRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(testUpPartyRequest)) return BadRequest(ModelState);

            var partyName = await partyLogic.GeneratePartyNameAsync(false);

            try
            {
                var secret = SecretGenerator.GenerateNewSecret();
                var codeVerifier = RandomGenerator.Generate(64);

                var authenticationRequest = new AuthenticationRequest
                {
                    ClientId = partyName,
                    ResponseMode = testUpPartyRequest.ResponseMode,
                    ResponseType = ResponseTypes.Code,
                    RedirectUri = testUpPartyRequest.RedirectUri,
                    Scope = DefaultOidcScopes.OpenId,
                    Nonce = RandomGenerator.GenerateNonce(),
                    State = $"{partyName}{stateSplitKey}{CreateProtector(partyName).Protect(secret)}"
                };

                var codeChallengeRequest = new CodeChallengeSecret
                {
                    CodeChallenge = await codeVerifier.Sha256HashBase64urlEncodedAsync(),
                    CodeChallengeMethod = CodeChallengeMethods.S256,
                };

                var requestDictionary = authenticationRequest.ToDictionary().AddToDictionary(codeChallengeRequest);
                var testUrl = QueryHelpers.AddQueryString(UrlCombine.Combine(GetAuthority(partyName), Constants.Routes.OAuthController, Constants.Endpoints.Authorize) , requestDictionary);

                var mParty = new OidcDownPartyTest
                {
                    Id = await DownParty.IdFormatAsync(RouteBinding, partyName),
                    IsTest = true,
                    TestUrl = testUrl,
                    Nonce = authenticationRequest.Nonce,
                    CodeVerifier = codeVerifier,
                    Name = await partyLogic.GeneratePartyNameAsync(false),
                    AllowUpParties = testUpPartyRequest.UpPartyNames.Select(pName => new UpPartyLink { Name = pName }).ToList(),
                    Client = new OidcDownClient
                    {
                        RedirectUris = new List<string> { authenticationRequest.RedirectUri },
                        ResponseTypes = new List<string> { authenticationRequest.ResponseType },
                        RequirePkce = true,
                        Claims = testUpPartyRequest.Claims.Select(c => new OidcDownClaim { Claim = c }).ToList(),
                        ResourceScopes = new List<OAuthDownResourceScope> { new OAuthDownResourceScope { Resource = partyName } },
                        DisableClientCredentialsGrant = true,
                        DisableTokenExchangeGrant = true,
                    },
                    TimeToLive = settings.DownPartyTestLifetime
                };

                var oauthClientSecret = new OAuthClientSecret();
                await secretHashLogic.AddSecretHashAsync(oauthClientSecret, secret);
                mParty.Client.Secrets = [oauthClientSecret];

                if (!await validateModelGenericPartyLogic.ValidateModelAllowUpPartiesAsync(ModelState, nameof(testUpPartyRequest.UpPartyNames), mParty)) return BadRequest(ModelState);

                await tenantDataRepository.CreateAsync(mParty);

                return Ok(new Api.DownPartyTestStartResponse
                {
                    TestUrl = testUrl,
                    ExpireAt = mParty.ExpireAt,
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
                    logger.Warning(ex, $"Conflict, Create '{typeof(OidcDownPartyTest).Name}' by name '{partyName}'.");
                    return Conflict(typeof(OidcDownPartyTest).Name, partyName, nameof(OidcDownPartyTest.Name));
                }
                throw;
            }
        }

        /// <summary>
        /// Get the down-party test result.
        /// </summary>
        /// <param name="TestUpPartyResultRequest">Down-party test result request.</param>
        /// <returns>Down-party test.</returns>
        [ProducesResponseType(typeof(Api.DownPartyTestResultResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<Api.DownPartyTestResultResponse>> PutDownPartyTest([FromBody] Api.DownPartyTestResultRequest testUpPartyRequest)
        {
            if (!await ModelState.TryValidateObjectAsync(testUpPartyRequest)) return BadRequest(ModelState);

            var stateSplit = testUpPartyRequest.State.Split(stateSplitKey);
            if (stateSplit.Length != 2)
            {
                throw new Exception("Invalid state format.");
            }

            var partyName = stateSplit[0];

            try
            {
                var secret = CreateProtector(partyName).Unprotect(stateSplit[1]);

                var mParty = await tenantDataRepository.GetAsync<OidcDownPartyTest>(await DownParty.IdFormatAsync(RouteBinding, partyName));

                (var tokenResponse, var idTokenPrincipal, var accessTokenPrincipal) = await AcquireTokensAsync(mParty, mParty.Nonce, testUpPartyRequest.Code);

                var testUpPartyResultResponse = new Api.DownPartyTestResultResponse
                {
                    IdTokenClaims = mapper.Map<List<Api.ClaimAndValues>>(idTokenPrincipal.Claims.ToClaimAndValues()),
                    AccessTokenClaims = mapper.Map<List<Api.ClaimAndValues>>(accessTokenPrincipal.Claims.ToClaimAndValues()),
                    IdToken = tokenResponse.IdToken,
                    AccessToken = tokenResponse.AccessToken,
                };
                return Ok(testUpPartyResultResponse);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (FoxIDsDataException ex)
            {
                if (ex.StatusCode == DataStatusCode.NotFound)
                {
                    logger.Warning(ex, $"NotFound, Update '{typeof(OidcDownPartyTest).Name}' by name '{partyName}'.");
                    return NotFound(typeof(OidcDownPartyTest).Name, partyName, nameof(OidcDownPartyTest.Name));
                }
                throw;
            }
        }
        private string GetAuthority(string partyName) => UrlCombine.Combine(settings.FoxIDsEndpoint, RouteBinding.TenantName, RouteBinding.TrackName, $"{partyName}(*)");

        private async Task<(TokenResponse tokenResponse, ClaimsPrincipal idTokenPrincipal, ClaimsPrincipal accessTokenPrincipal)> AcquireTokensAsync(OidcDownPartyTest mParty, string nonce, string code)
        {
            var tokenRequest = new TokenRequest
            {
                GrantType = GrantTypes.AuthorizationCode,
                Code = code,
                ClientId = mParty.Name,
                RedirectUri = mParty.Client.RedirectUris.First(),
            };

            var codeVerifierSecret = new CodeVerifierSecret
            {
                CodeVerifier = mParty.CodeVerifier,
            };

            (var oidcDiscovery, var jsonWebKeySet) = await oidcDiscoveryReadLogic.GetOidcDiscoveryAndValidateAsync(GetAuthority(mParty.Name));

            var requestDictionary = tokenRequest.ToDictionary().AddToDictionary(codeVerifierSecret);

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

                case HttpStatusCode.BadRequest:
                    var resultBadRequest = await response.Content.ReadAsStringAsync();
                    var tokenResponseBadRequest = resultBadRequest.ToObject<TokenResponse>();
                    tokenResponseBadRequest.Validate(true);
                    throw new Exception($"Error login call back, Bad request. StatusCode={response.StatusCode}");

                default:
                    throw new Exception($"Error login call back, Status Code not expected. StatusCode={response.StatusCode}");
            }
        }

        private IDataProtector CreateProtector(string partyName)
        {
            return dataProtection.CreateProtector(new[] { RouteBinding.TenantName, RouteBinding.TrackName, partyName });
        }
    }
}
