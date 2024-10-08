﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Ext = FoxIDs.Models.ExternalLogin;
using System.Net.Mime;

namespace FoxIDs.Logic
{
    public class ExternalLoginConnectLogic : LogicSequenceBase
    {
        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly TelemetryScopedLogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly FailingLoginLogic failingLoginLogic;

        public ExternalLoginConnectLogic(TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, FailingLoginLogic failingLoginLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.failingLoginLogic = failingLoginLogic;
        }

        public async Task<List<Claim>> ValidateUserAsync(ExternalLoginUpParty party, ExternalLoginUpPartyProfile profile, string username, string password)
        {
            var claims = party.ExternalLoginType switch
            {
                ExternalLoginTypes.Api => await ValidateUserApiAsync(party, profile, username, password),
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

        private async Task<List<Claim>> ValidateUserApiAsync(ExternalLoginUpParty extLoginUpParty, ExternalLoginUpPartyProfile profile, string username, string password)
        {
            var authenticationApiUrl = UrlCombine.Combine(extLoginUpParty.ApiUrl, Constants.ExternalLogin.Api.Authentication);
            logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API request, URL '{authenticationApiUrl}'.", traceType: TraceTypes.Message);

            var authRequest = new Ext.AuthenticationRequest
            {
                UsernameType = extLoginUpParty.UsernameType,
                Username = username,
                Password = password
            };
            logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API request '{new { authRequest.UsernameType, authRequest.Username }.ToJson()}'.", traceType: TraceTypes.Message);
            var requestDictionary = authRequest.ToDictionary();

            var additionalParameters = new List<OAuthAdditionalParameter>();
            if (extLoginUpParty.AdditionalParameters?.Count() > 0)
            {
                foreach (var additionalParameter in extLoginUpParty.AdditionalParameters)
                {
                    additionalParameters.Add(additionalParameter);
                }
            }
            if (profile != null && profile.AdditionalParameters?.Count() > 0)
            {
                foreach (var additionalParameter in profile.AdditionalParameters)
                {
                    var item = additionalParameters.Where(a => a.Name == additionalParameter.Name).FirstOrDefault();
                    if (item != null)
                    {
                        item.Value = additionalParameter.Value;
                    }
                    else
                    {
                        additionalParameters.Add(additionalParameter);
                    }
                }
            }

            if (additionalParameters.Count() > 0)
            {
                foreach (var additionalParameter in additionalParameters)
                {
                    if (!requestDictionary.ContainsKey(additionalParameter.Name))
                    {
                        requestDictionary.Add(additionalParameter.Name, additionalParameter.Value);
                    }
                }
                logger.ScopeTrace(() => $"AuthMethod, External login, AdditionalParameters request '{{{string.Join(", ", additionalParameters.Select(p => $"\"{p.Name}\": \"{p.Value}\""))}}}'.", traceType: TraceTypes.Message);
            }

            var httpClient = httpClientFactory.CreateClient();
            logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API secret '{(extLoginUpParty.Secret?.Length > 10 ? extLoginUpParty.Secret.Substring(0, 3) : string.Empty)}'.", traceType: TraceTypes.Message);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BasicAuthentication.Basic, $"{Constants.ExternalLogin.Api.ApiId.OAuthUrlDencode()}:{extLoginUpParty.Secret.OAuthUrlDencode()}".Base64Encode());

            var failingLoginCount = await failingLoginLogic.VerifyFailingLoginCountAsync(username, isExternalLogin: true);
            
            var content = new StringContent(JsonConvert.SerializeObject(requestDictionary, jsonSerializerSettings), Encoding.UTF8, MediaTypeNames.Application.Json);
            using var response = await httpClient.PostAsync(authenticationApiUrl, content);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var authenticationResponse = result.ToObject<Ext.AuthenticationResponse>();
                    logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API response '{authenticationResponse.ToJson()}'.", traceType: TraceTypes.Message);

                    await failingLoginLogic.ResetFailingLoginCountAsync(username, isExternalLogin: true);
                    logger.ScopeTrace(() => $"AuthMethod, External login, User '{username}' and password valid.", scopeProperties: failingLoginLogic.FailingLoginCountDictonary(failingLoginCount), triggerEvent: true);

                    return authenticationResponse.Claims?.Select(c => new Claim(c.Type, c.Value))?.ToList();

                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    var resultError = await response.Content.ReadAsStringAsync();
                    var errorResponse = resultError.ToObject<Ext.ErrorResponse>();
                    logger.ScopeTrace(() => $"AuthMethod, External login, Authentication API error '{resultError}'. Status code={response.StatusCode}.", traceType: TraceTypes.Message);

                    if (errorResponse.Error == Constants.ExternalLogin.Api.ErrorCodes.InvalidUsernameOrPassword)
                    {
                        throw new InvalidUsernameOrPasswordException($"Username or password invalid, user '{username}'.");
                    }
                    throw new Exception($"AuthMethod, External login, Authentication API error '{resultError}'. Status code={response.StatusCode}.");

                default:
                    var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                    throw new Exception($"AuthMethod, External login, Authentication API error '{resultUnexpectedStatus}'. Status code={response.StatusCode}.");
            }
        }
    }
}
