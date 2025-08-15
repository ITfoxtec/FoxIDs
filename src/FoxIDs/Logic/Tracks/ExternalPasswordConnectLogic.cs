using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Ext = FoxIDs.Models.External;

namespace FoxIDs.Logic
{
    public class ExternalPasswordConnectLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpClientFactory httpClientFactory;

        public ExternalPasswordConnectLogic(TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task ValidatePasswordAsync(UserIdentifier userIdentifier, string password, PasswordState state)
        {
            if (Config?.EnabledValidation != true) return;

            var url = UrlCombine.Combine(Config.ApiUrl, Constants.ExternalConnect.ExternalPassword.Api.Validation);
            logger.ScopeTrace(() => $"External password, Validation API request, URL '{url}'.", traceType: TraceTypes.Message);

            var request = new Ext.PasswordRequest
            {
                Email = userIdentifier.Email,
                Phone = userIdentifier.Phone,
                Username = userIdentifier.Username,
                Password = password,
                State = ToExtPasswordState(state)
            };
            await request.ValidateObjectAsync();
            logger.ScopeTrace(() => $"External password, Validation API request '{new { request.Email, request.Phone, request.Username, password = "hidden" }.ToJson()}'.", traceType: TraceTypes.Message);

            var httpClient = httpClientFactory.CreateClient();
            logger.ScopeTrace(() => $"External password, Validation API secret '{(Config.Secret?.Length > 10 ? $"{Config.Secret.Substring(0, 3)}..." : "hidden")}'.", traceType: TraceTypes.Message);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BasicAuthentication.Basic, $"{Constants.ExternalConnect.ExternalPassword.Api.ApiId.OAuthUrlDencode()}:{Config.Secret.OAuthUrlDencode()}".Base64Encode());

            try
            {
                using var response = await httpClient.PostAsPlainJsonAsync(url, request);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var result = await response.Content.ReadAsStringAsync();
                        logger.ScopeTrace(() => $"External password, Validation API response '{(result.IsNullOrWhiteSpace() ? "OK" : result)}'.", traceType: TraceTypes.Message);
                        return;

                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        var resultError = await response.Content.ReadAsStringAsync();
                        var errorResponse = resultError.ToObject<Ext.ErrorResponse>();
                        await errorResponse.ValidateObjectAsync();
                        logger.ScopeTrace(() => $"External password, Validation API error '{resultError}'. Status code={response.StatusCode}.", traceType: TraceTypes.Message);

                        if (errorResponse.Error == Constants.ExternalConnect.Api.ErrorCodes.InvalidApiIdOrSecret)
                        {
                            throw new InvalidAppIdOrSecretException($"Invalid app id '{Constants.ExternalConnect.ExternalPassword.Api.ApiId}' or secret '{(Config.Secret?.Length > 10 ? $"{Config.Secret.Substring(0, 3)}..." : "hidden")}', API URL '{url}'. Status code={response.StatusCode}.{errorResponse.GetErrorMessage()}");
                        }

                        if (errorResponse.Error == Constants.ExternalConnect.ExternalPassword.Api.ErrorCodes.PasswordNotAccepted)
                        {
                            var passwordNotAcceptedExternalException = new PasswordNotAcceptedExternalException($"Password not accepted, user '{new { request.Email, request.Phone, request.Username }.ToJson()}', API URL '{url}'.{errorResponse.GetErrorMessage()}");
                            if (!errorResponse.UiErrorMessage.IsNullOrWhiteSpace())
                            {
                                passwordNotAcceptedExternalException.UiErrorMessages.Add(errorResponse.UiErrorMessage);
                            }
                            throw passwordNotAcceptedExternalException;
                        }

                        throw new Exception($"External password, Validation API error '{resultError}'. Status code={response.StatusCode}.{errorResponse.GetErrorMessage()}");

                    default:
                        var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                        resultUnexpectedStatus.ValidateMaxLength(Constants.ExternalConnect.ErrorMessageLength, nameof(resultUnexpectedStatus), nameof(ExternalPasswordConnectLogic));
                        throw new Exception($"External password, Validation API error '{resultUnexpectedStatus}'. Status code={response.StatusCode}.");
                }
            }
            catch (InvalidAppIdOrSecretException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to call external password validation API URL '{url}'.", ex);
            }
        }

        public async Task PasswordNotificationAsync(UserIdentifier userIdentifier, string password, PasswordState state)
        {
            if (Config?.EnabledNotification != true) return;

            var url = UrlCombine.Combine(Config.ApiUrl, Constants.ExternalConnect.ExternalPassword.Api.Notification);
            logger.ScopeTrace(() => $"External password, Notification API request, URL '{url}'.", traceType: TraceTypes.Message);

            var request = new Ext.PasswordRequest
            {
                Email = userIdentifier.Email,
                Phone = userIdentifier.Phone,
                Username = userIdentifier.Username,
                Password = password,
                State = ToExtPasswordState(state)
            };
            await request.ValidateObjectAsync();
            logger.ScopeTrace(() => $"External password, Notification API request '{new { request.Email, request.Phone, request.Username, password = "hidden" }.ToJson()}'.", traceType: TraceTypes.Message);

            var httpClient = httpClientFactory.CreateClient();
            logger.ScopeTrace(() => $"External password, Notification API secret '{(Config.Secret?.Length > 10 ? $"{Config.Secret.Substring(0, 3)}..." : "hidden")}'.", traceType: TraceTypes.Message);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BasicAuthentication.Basic, $"{Constants.ExternalConnect.ExternalPassword.Api.ApiId.OAuthUrlDencode()}:{Config.Secret.OAuthUrlDencode()}".Base64Encode());

            try
            {
                using var response = await httpClient.PostAsPlainJsonAsync(url, request);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var result = await response.Content.ReadAsStringAsync();
                        logger.ScopeTrace(() => $"External password, Notification API response '{(result.IsNullOrWhiteSpace() ? "OK" : result)}'.", traceType: TraceTypes.Message);
                        return;

                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        var resultError = await response.Content.ReadAsStringAsync();
                        var errorResponse = resultError.ToObject<Ext.ErrorResponse>();
                        await errorResponse.ValidateObjectAsync();
                        logger.ScopeTrace(() => $"External password, Notification API error '{resultError}'. Status code={response.StatusCode}.", traceType: TraceTypes.Message);

                        if (errorResponse.Error == Constants.ExternalConnect.Api.ErrorCodes.InvalidApiIdOrSecret)
                        {
                            throw new InvalidAppIdOrSecretException($"Invalid app id '{Constants.ExternalConnect.ExternalPassword.Api.ApiId}' or secret '{(Config.Secret?.Length > 10 ? $"{Config.Secret.Substring(0, 3)}..." : "hidden")}', API URL '{url}'. Status code={response.StatusCode}.{errorResponse.GetErrorMessage()}");
                        }
                        throw new Exception($"External password, Notification API error '{resultError}'. Status code={response.StatusCode}.{errorResponse.GetErrorMessage()}");

                    default:
                        var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                        resultUnexpectedStatus.ValidateMaxLength(Constants.ExternalConnect.ErrorMessageLength, nameof(resultUnexpectedStatus), nameof(ExternalPasswordConnectLogic));
                        throw new Exception($"External password, Notification API error '{resultUnexpectedStatus}'. Status code={response.StatusCode}.");
                }
            }
            catch (InvalidAppIdOrSecretException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to call external password notification API URL '{url}'.", ex);
            }
        }

        private ExternalPassword Config => RouteBinding.ExternalPassword;

        private Ext.PasswordState ToExtPasswordState(PasswordState state)
        {
            switch (state)
            {
                case PasswordState.Current:
                    return Ext.PasswordState.Current;
                case PasswordState.New:
                    return Ext.PasswordState.New;
                default:
                    throw new NotImplementedException($"Password state '{state}' is not implemented in {nameof(ExternalPasswordConnectLogic)}.");
            }
        }

    }
}
