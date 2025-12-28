using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.HttpClientFactory;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Modules;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
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
        private readonly IMtlsHttpClientFactory httpClientFactory;

        public NemLoginSubjectMatchesCprLogic(Settings settings, TelemetryScopedLogger logger, TrackKeyLogic trackKeyLogic, IMtlsHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.trackKeyLogic = trackKeyLogic;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<bool> SubjectMatchesCprAsync(NemLoginEnvironments environment, string cprNumber, string subjectNameId, string entityId, CancellationToken cancellationToken)
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
