using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.ViewModels;
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
    public class ExtendedUiConnectLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpClientFactory httpClientFactory;

        public ExtendedUiConnectLogic(TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<List<Claim>> ValidateElementsAsync(ExtendedUi extendedUi, List<Claim> claims, List<DynamicElementBase> elements)
        {
            var externalClaims = extendedUi.ExternalConnectType switch
            {
                ExternalConnectTypes.Api => await ValidateElementsApiAsync(extendedUi, claims, elements),
                _ => throw new NotSupportedException()
            };

            externalClaims = externalClaims ?? new List<Claim>();           
            logger.ScopeTrace(() => $"AuthMethod, Extended UI, received external claims '{externalClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return externalClaims;
        }

        private async Task<List<Claim>> ValidateElementsApiAsync(ExtendedUi extendedUi, List<Claim> claims, List<DynamicElementBase> elements)
        {
            var extendedUiApiUrl = UrlCombine.Combine(extendedUi.ApiUrl, Constants.ExternalConnect.ExtendedUi.Api.Validate);
            logger.ScopeTrace(() => $"AuthMethod, Extended UI, Validate API request, URL '{extendedUiApiUrl}'.", traceType: TraceTypes.Message);

            var extendedUiRequest = new Ext.ExtendedUiRequest
            {
                Claims = claims?.Select(c => new Ext.ClaimValue { Type = c.Type, Value = c.Value }),
                Elements = GetElements(elements).Where(e => e != null)
            };
            await extendedUiRequest.ValidateObjectAsync();
            logger.ScopeTrace(() => $"AuthMethod, Extended UI, Validate API request '{extendedUiRequest.ToJson()}'.", traceType: TraceTypes.Message);

            var httpClient = httpClientFactory.CreateClient();
            logger.ScopeTrace(() => $"AuthMethod, Extended UI, Validate API secret '{(extendedUi.Secret?.Length > 10 ? $"{extendedUi.Secret.Substring(0, 3)}..." : "hidden")}'.", traceType: TraceTypes.Message);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BasicAuthentication.Basic, $"{Constants.ExternalConnect.ExtendedUi.Api.ApiId.OAuthUrlDencode()}:{extendedUi.Secret.OAuthUrlDencode()}".Base64Encode());

            try
            {
                using var response = await httpClient.PostAsPlainJsonAsync(extendedUiApiUrl, extendedUiRequest);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var result = await response.Content.ReadAsStringAsync();
                        var extendedUiResponse = result.ToObject<Ext.ExtendedUiResponse>();
                        await extendedUiResponse.ValidateObjectAsync();
                        logger.ScopeTrace(() => $"AuthMethod, Extended UI, Validate API response '{extendedUiResponse.ToJson()}'.", traceType: TraceTypes.Message);
                        return extendedUiResponse.Claims?.Select(c => new Claim(c.Type, c.Value))?.ToList();

                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        var resultError = await response.Content.ReadAsStringAsync();
                        var errorResponse = resultError.ToObject<Ext.ErrorResponse>();
                        await errorResponse.ValidateObjectAsync();
                        logger.ScopeTrace(() => $"AuthMethod, Extended UI, Validate API error '{resultError}'. Status code={response.StatusCode}.", traceType: TraceTypes.Message);

                        if (errorResponse.Error == Constants.ExternalConnect.Api.ErrorCodes.InvalidApiIdOrSecret)
                        {
                            throw new InvalidAppIdOrSecretException($"Invalid app id '{Constants.ExternalConnect.ExtendedUi.Api.ApiId}' or secret '{(extendedUi.Secret?.Length > 10 ? $"{extendedUi.Secret.Substring(0, 3)}..." : "hidden")}', API URL '{extendedUiApiUrl}'. Status code={response.StatusCode}.{errorResponse.GetErrorMessage()}");
                        }
                        else if (errorResponse.Error == Constants.ExternalConnect.ExtendedUi.Api.ErrorCodes.Invalid)
                        {
                            var invalidElementsException = new InvalidElementsException($"Invalid elements, API URL '{extendedUiApiUrl}'.{errorResponse.GetErrorMessage()}")
                            {
                                UiErrorMessages = new List<string>(),
                                Elements = new List<ElementError>()
                            };

                            if (!errorResponse.UiErrorMessage.IsNullOrWhiteSpace())
                            {
                                invalidElementsException.UiErrorMessages.Add(errorResponse.UiErrorMessage);
                            }

                            var ghostElementUiErrorMessages = new List<string>();
                            if (errorResponse.Elements?.Count() > 0)
                            {
                                foreach(var errorElement in errorResponse.Elements)
                                {
                                    var element = elements.FirstOrDefault(e => e.Name == errorElement.Name);
                                    if (element != null)
                                    {
                                        if (!errorElement.UiErrorMessage.IsNullOrWhiteSpace())
                                        {
                                            invalidElementsException.Elements.Add(new ElementError { Name = errorElement.Name, UiErrorMessage = errorElement.UiErrorMessage });
                                        }
                                        else if (element is CustomDElement customDElement && !customDElement.ErrorMessage.IsNullOrWhiteSpace())
                                        {
                                            invalidElementsException.Elements.Add(new ElementError { Name = errorElement.Name, UiErrorMessage = customDElement.ErrorMessage });
                                        }
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(errorElement.UiErrorMessage))
                                        {
                                            ghostElementUiErrorMessages.Add(errorElement.UiErrorMessage);
                                        }
                                    }
                                }
                            }

                            if (!invalidElementsException.UiErrorMessages.Any() && (ghostElementUiErrorMessages.Any() || !invalidElementsException.Elements.Any()))
                            {
                                invalidElementsException.UiErrorMessages.Add(extendedUi.ErrorMessage);
                            }
                            if (ghostElementUiErrorMessages.Any())
                            {
                                invalidElementsException.UiErrorMessages.AddRange(ghostElementUiErrorMessages);
                            }

                            throw invalidElementsException;
                        }
                        throw new Exception($"AuthMethod, Extended UI, Validate API error '{resultError}'. Status code={response.StatusCode}.{errorResponse.GetErrorMessage()}");

                    default:
                        var resultUnexpectedStatus = await response.Content.ReadAsStringAsync();
                        resultUnexpectedStatus.ValidateMaxLength(Constants.ExternalConnect.ErrorMessageLength, nameof(resultUnexpectedStatus), nameof(ExtendedUiConnectLogic));
                        throw new Exception($"AuthMethod, Extended UI, Validate API error '{resultUnexpectedStatus}'. Status code={response.StatusCode}.");
                }
            }
            catch (InvalidAppIdOrSecretException)
            {
                throw;
            }
            catch (InvalidElementsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to call external extended UI API URL '{extendedUiApiUrl}'.", ex);
            }
        }

        private IEnumerable<Ext.ElementValue> GetElements(List<DynamicElementBase> elements)
        {
            foreach (var element in elements)
            {
                yield return element switch
                {
                    EmailDElement => GetElementValue(element, GetTypeString(Ext.ElementTypes.Email), JwtClaimTypes.Email),
                    PhoneDElement => GetElementValue(element, GetTypeString(Ext.ElementTypes.Phone), JwtClaimTypes.PhoneNumber, value: element.DField2), // Full phone only (DField2)
                    UsernameDElement => GetElementValue(element, GetTypeString(Ext.ElementTypes.Username), JwtClaimTypes.PreferredUsername),
                    NameDElement => GetElementValue(element, GetTypeString(Ext.ElementTypes.Name), JwtClaimTypes.Name),
                    GivenNameDElement => GetElementValue(element, GetTypeString(Ext.ElementTypes.GivenName), JwtClaimTypes.GivenName),
                    FamilyNameDElement => GetElementValue(element, GetTypeString(Ext.ElementTypes.FamilyName), JwtClaimTypes.FamilyName),
                    CustomDElement => GetElementValue(element, GetTypeString(Ext.ElementTypes.Custom), (element as CustomDElement).ClaimOut),
                    _ => null
                };
            }
        }

        private string GetTypeString(Ext.ElementTypes type) => type.ToString();

        private Ext.ElementValue GetElementValue(DynamicElementBase element, string type, string claimType, string value = null)
        {
            return new Ext.ElementValue { Name = element.Name, Value = value ?? element.DField1, Type = type, ClaimType = claimType };
        }
    }
}
