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
using System.Security.Claims;
using System.Threading.Tasks;
using Ext = FoxIDs.Models.External;

namespace FoxIDs.Logic
{
    public class ExternalClaimsConnectLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpClientFactory httpClientFactory;

        public ExternalClaimsConnectLogic(TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<List<Claim>> GetClaimsAsync(ClaimTransform claimTransform, List<Claim> claims)
        {
            var claimsResult = claimTransform.ExternalConnectType switch
            {
                ExternalConnectTypes.Api => await GetClaimsApiAsync(claimTransform, claims),
                _ => throw new NotSupportedException()
            };

            claimsResult = claimsResult ?? new List<Claim>();           
            logger.ScopeTrace(() => $"Transform claims, External claims, received claims '{claimsResult.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return claimsResult;
        }

        private async Task<List<Claim>> GetClaimsApiAsync(ClaimTransform claimTransform, List<Claim> claims)
        {
            var claimsApiUrl = UrlCombine.Combine(claimTransform.ApiUrl, Constants.ExternalConnect.ExternalClaims.Api.Claims);
            logger.ScopeTrace(() => $"Transform claims, External claims, Claims API request, URL '{claimsApiUrl}'.", traceType: TraceTypes.Message);

            var claimsRequest = new Ext.ClaimsRequest
            {
                Claims = claims?.Select(c => new Ext.ClaimValue { Type = c.Type, Value = c.Value }),
            };
            await claimsRequest.ValidateObjectAsync();
            logger.ScopeTrace(() => $"Transform claims, External claims, Claims API request '{claimsRequest.ToJson()}'.", traceType: TraceTypes.Message);

            var httpClient = httpClientFactory.CreateClient();
            logger.ScopeTrace(() => $"Transform claims, External claims, Claims API secret '{(claimTransform.Secret?.Length > 10 ? $"{claimTransform.Secret.Substring(0, 3)}..." : "hidden")}'.", traceType: TraceTypes.Message);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BasicAuthentication.Basic, $"{Constants.ExternalConnect.ExternalClaims.Api.ApiId.OAuthUrlDencode()}:{claimTransform.Secret.OAuthUrlDencode()}".Base64Encode());

            try
            {
                using var response = await httpClient.PostAsPlainJsonAsync(claimsApiUrl, claimsRequest);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var result = await response.Content.ReadAsStringAsync();
                        var claimsResponse = result.ToObject<Ext.ClaimsResponse>();
                        await claimsResponse.ValidateObjectAsync();
                        logger.ScopeTrace(() => $"Transform claims, External claims, Claims API response '{claimsResponse.ToJson()}'.", traceType: TraceTypes.Message);
                        return claimsResponse.Claims?.Select(c => new Claim(c.Type, c.Value))?.ToList();

                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        var resultError = await response.Content.ReadAsStringAsync();
                        var errorResponse = resultError.ToObject<Ext.ErrorResponse>();
                        await errorResponse.ValidateObjectAsync();
                        logger.ScopeTrace(() => $"Transform claims, External claims, Claims API error '{resultError}'. Status code={response.StatusCode}.", traceType: TraceTypes.Message);

                        if (errorResponse.Error == Constants.ExternalConnect.Api.ErrorCodes.InvalidApiIdOrSecret)
                        {
                            throw new InvalidAppIdOrSecretException($"Invalid app id '{Constants.ExternalConnect.ExternalClaims.Api.ApiId}' or secret '{(claimTransform.Secret?.Length > 10 ? $"{claimTransform.Secret.Substring(0, 3)}..." : "hidden")}', API URL '{claimsApiUrl}'. Status code={response.StatusCode}.{errorResponse.GetErrorMessage()}");
                        }
                        throw new Exception($"Transform claims, External claims, Claims API error '{resultError}'. Status code={response.StatusCode}.");

                    default:
                        var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                        resultUnexpectedStatus.ValidateMaxLength(Constants.ExternalConnect.ErrorMessageLength, nameof(resultUnexpectedStatus), nameof(ExternalClaimsConnectLogic));
                        throw new Exception($"Transform claims, External claims, Claims API error '{resultUnexpectedStatus}'. Status code={response.StatusCode}.");
                }
            }
            catch (InvalidAppIdOrSecretException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to call external claims API URL '{claimsApiUrl}'.", ex);
            }
        }
    }
}
