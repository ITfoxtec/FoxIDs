using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.HttpClientFactory;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Modules;
using FoxIDs.Models.ViewModels;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class NemLoginSubjectMatchesCprLogic : LogicSequenceBase
    {
        private const string MatchStatus = "Match";

        private readonly Settings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly TrackKeyLogic trackKeyLogic;
        private readonly DynamicElementLogic dynamicElementLogic;
        private readonly TrackIssuerLogic trackIssuerLogic;
        private readonly IMtlsHttpClientFactory httpClientFactory;

        public NemLoginSubjectMatchesCprLogic(Settings settings, TelemetryScopedLogger logger, TrackKeyLogic trackKeyLogic, DynamicElementLogic dynamicElementLogic, TrackIssuerLogic trackIssuerLogic, IMtlsHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.trackKeyLogic = trackKeyLogic;
            this.dynamicElementLogic = dynamicElementLogic;
            this.trackIssuerLogic = trackIssuerLogic;
            this.httpClientFactory = httpClientFactory;
        }

        public void PopulateExtendedUi(ExtendedUi extendedUi)
        {
            extendedUi.Title = "Enter CPR number";
            extendedUi.SubmitButtonText = "Continue";

            if (extendedUi.Elements?.Any() != true)
            {
                extendedUi.Elements = new List<DynamicElement>
                {
                    new DynamicElement
                    {
                        Name = "cpr_info",
                        Type = DynamicElementTypes.Text,
                        Order = 1,
                        Content = "Please enter your CPR number to continue."
                    },
                    new DynamicElement
                    {
                        Name = Constants.Modules.Nemlogin.ExtendedUiCprElementName,
                        Type = DynamicElementTypes.Custom,
                        Order = 2,
                        Required = true,
                        DisplayName = "CPR number",
                        MaxLength = 20,
                        RegEx = @"^\s*\d{6}[- ]?\d{4}\s*$",
                        ErrorMessage = "Invalid CPR number format.",
                        ClaimOut = Constants.JwtClaimTypes.CprNumber
                    }
                };
            }
        }

        public async Task<IActionResult> HandleInputAsync(UpParty extendedUiUpParty, ExtendedUi extendedUi, ExtendedUiViewModel extendedUiViewModel, List<Claim> claims, ModelStateDictionary modelState, Func<IActionResult> viewError)
        {
            var cprInput = GetCprValue(extendedUiViewModel.InputElements);
            if (cprInput.IsNullOrWhiteSpace())
            {
                dynamicElementLogic.SetModelElementError(modelState, extendedUiViewModel.InputElements, Constants.Modules.Nemlogin.ExtendedUiCprElementName, "CPR number is required.");
                return viewError();
            }

            var subjectNameId = GetSubjectNameId(claims);
            if (subjectNameId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException($"Unable to locate subject name identifier in claim '{JwtClaimTypes.Subject}'.");
            }

            var entityId = !extendedUiUpParty.SpIssuer.IsNullOrWhiteSpace() ? extendedUiUpParty.SpIssuer : trackIssuerLogic.GetIssuer();

            var normalizedCprNumber = NormalizeCprNumber(cprInput);
            if (normalizedCprNumber.IsNullOrWhiteSpace())
            {
                dynamicElementLogic.SetModelElementError(modelState, extendedUiViewModel.InputElements, Constants.Modules.Nemlogin.ExtendedUiCprElementName, "Invalid CPR number format.");
                return viewError();
            }

            try
            {
                var isMatch = await SubjectMatchesCprAsync(extendedUi.Modules.NemLogin.Environment, normalizedCprNumber, subjectNameId, entityId, HttpContext.RequestAborted);
                if (!isMatch)
                {
                    dynamicElementLogic.SetModelElementError(modelState, extendedUiViewModel.InputElements, Constants.Modules.Nemlogin.ExtendedUiCprElementName, "CPR number does not match the user.");
                    return viewError();
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"NemLog-in SubjectMatchesCPR failed. SubjectNameId '{subjectNameId}', EntityId '{entityId}'.", ex) { RouteBinding = RouteBinding };
            }

            claims.AddOrReplaceClaim(Constants.JwtClaimTypes.CprNumber, normalizedCprNumber);
            return null;
        }

        private static string GetCprValue(List<DynamicElementBase> inputElements)
        {
            return inputElements?.OfType<CustomDElement>()?.Where(e => e.Name == Constants.Modules.Nemlogin.ExtendedUiCprElementName).Select(e => e.DField1).FirstOrDefault();
        }

        private static string NormalizeCprNumber(string cprNumber)
        {
            if (cprNumber.IsNullOrWhiteSpace())
            {
                return null;
            }

            var digitsOnly = new string(cprNumber.Where(char.IsDigit).ToArray());
            return digitsOnly.Length == 10 ? digitsOnly : null;
        }

        private static string GetSubjectNameId(List<Claim> claims)
        {
            var subject = claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject);

            if (subject.IsNullOrWhiteSpace())
            {
                return null;
            }

            var delimiterIndex = subject.IndexOf('|');
            return delimiterIndex > -1 && subject.Length > delimiterIndex + 1 ? subject.Substring(delimiterIndex + 1) : subject;
        }

        private async Task<bool> SubjectMatchesCprAsync(NemLoginEnvironments environment, string cprNumber, string subjectNameId, string entityId, CancellationToken cancellationToken)
        {
            if (cprNumber.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(cprNumber));
            if (subjectNameId.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(subjectNameId));
            if (entityId.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(entityId));

            var apiUrl = environment switch
            {
                NemLoginEnvironments.Production => settings.Modules?.NemLogin?.SubjectMatchesCpr?.ProductionApiUrl,
                NemLoginEnvironments.IntegrationTest => settings.Modules?.NemLogin?.SubjectMatchesCpr?.IntegrationTestApiUrl,
                _ => throw new NotSupportedException($"NemLog-in SubjectMatchesCPR environment '{environment}' not supported.")
            };

            if (apiUrl.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException($"NemLog-in SubjectMatchesCPR API URL is not configured for '{environment}'.");
            }

            using var clientCertificate = await trackKeyLogic.GetPrimaryMtlsX509CertificateAsync(RouteBinding.Key);
            using var httpClient = httpClientFactory.CreateClient(clientCertificate);
            logger.ScopeTrace(() => $"AuthMethod, NemLog-in SubjectMatchesCPR request, URL '{apiUrl}'.", traceType: TraceTypes.Message);

            var request = new NemLoginSubjectMatchesCprRequest
            {
                Cpr = cprNumber,
                SubjectNameID = subjectNameId,
                EntityID = entityId,
            };
            logger.ScopeTrace(() => $"AuthMethod, NemLog-in SubjectMatchesCPR request '{new NemLoginSubjectMatchesCprRequest { Cpr = request.Cpr.MaskCprNumber(), SubjectNameID = request.SubjectNameID, EntityID = request.EntityID }.ToJson()}'.", traceType: TraceTypes.Message);

            try
            {
                using var response = await httpClient.PostAsPlainJsonAsync(apiUrl, request);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        var status = (responseBody ?? string.Empty).Trim().Trim('"');
                        if (status.IsNullOrWhiteSpace())
                        {
                            throw new Exception("NemLog-in SubjectMatchesCPR returned an empty response body.");
                        }

                        logger.ScopeTrace(() => $"AuthMethod, NemLog-in SubjectMatchesCPR response '{status}'.", triggerEvent: true, traceType: TraceTypes.Message);
                        return MatchStatus.Equals(status, StringComparison.OrdinalIgnoreCase);

                    default:
                        var responseError = await response.Content.ReadAsStringAsync(cancellationToken);
                        responseError.ValidateMaxLength(Constants.ExternalConnect.ErrorMessageLength, nameof(responseError), nameof(NemLoginSubjectMatchesCprLogic));
                        throw new Exception($"NemLog-in SubjectMatchesCPR returned unexpected status code={response.StatusCode}. Response '{responseError}'.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to call NemLog-in SubjectMatchesCPR API URL '{apiUrl}'.", ex);
            }
        }
    }
}
