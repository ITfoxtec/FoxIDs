using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Modules;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.MockHelpers;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FoxIDs.UnitTests.Logic.Modules
{
    public class NemLoginSubjectMatchesCprLogicTests
    {
        [Fact]
        public async Task SubjectMatchesCprAsync_Production_Match_ReturnsTrue()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant1", TrackName = "track1" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var logger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "\"Match\"");
            var httpClient = new HttpClient(handler);
            var httpClientFactory = new StubNemLoginHttpClientFactory(httpClient);

            var settings = new FoxIDsSettings
            {
                Modules = new ModulesSettings
                {
                    NemLogin = new NemLoginSettings
                    {
                        SubjectMatchesCpr = new NemLoginSubjectMatchesCprSettings
                        {
                            ProductionApiUrl = "https://services.nemlog-in.dk/api/uuidmatch/subjectmatchescpr",
                            IntegrationTestApiUrl = "https://services.test-nemlog-in.dk/api/uuidmatch/subjectmatchescpr"
                        }
                    }
                }
            };

            var logic = new NemLoginSubjectMatchesCprLogic(settings, logger, httpClientFactory);

            using var certificate = CreateTestCertificate();
            var isMatch = await logic.SubjectMatchesCprAsync(
                clientCertificate: certificate,
                environment: NemLoginEnvironments.Production,
                cprNumber: "0101011234",
                subjectNameId: "uuid-1",
                entityId: "entity-1",
                cancellationToken: CancellationToken.None);

            Assert.True(isMatch);
            Assert.Equal(settings.Modules.NemLogin.SubjectMatchesCpr.ProductionApiUrl, handler.RequestUri?.ToString());
            Assert.Contains("\"cpr\":\"0101011234\"", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"subjectNameID\":\"uuid-1\"", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"entityID\":\"entity-1\"", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SubjectMatchesCprAsync_IntegrationTest_NoMatch_ReturnsFalse()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant1", TrackName = "track1" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var logger = TelemetryLoggerHelper.ScopedLoggerObject(httpContextAccessor);

            var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "\"NoMatch\"");
            var httpClient = new HttpClient(handler);
            var httpClientFactory = new StubNemLoginHttpClientFactory(httpClient);

            var settings = new FoxIDsSettings
            {
                Modules = new ModulesSettings
                {
                    NemLogin = new NemLoginSettings
                    {
                        SubjectMatchesCpr = new NemLoginSubjectMatchesCprSettings
                        {
                            ProductionApiUrl = "https://services.nemlog-in.dk/api/uuidmatch/subjectmatchescpr",
                            IntegrationTestApiUrl = "https://services.test-nemlog-in.dk/api/uuidmatch/subjectmatchescpr"
                        }
                    }
                }
            };

            var logic = new NemLoginSubjectMatchesCprLogic(settings, logger, httpClientFactory);

            using var certificate = CreateTestCertificate();
            var isMatch = await logic.SubjectMatchesCprAsync(
                clientCertificate: certificate,
                environment: NemLoginEnvironments.IntegrationTest,
                cprNumber: "0101011234",
                subjectNameId: "uuid-1",
                entityId: "entity-1",
                cancellationToken: CancellationToken.None);

            Assert.False(isMatch);
            Assert.Equal(settings.Modules.NemLogin.SubjectMatchesCpr.IntegrationTestApiUrl, handler.RequestUri?.ToString());
        }

        private sealed class StubNemLoginHttpClientFactory : INemLoginHttpClientFactory
        {
            private readonly HttpClient httpClient;

            public StubNemLoginHttpClientFactory(HttpClient httpClient)
            {
                this.httpClient = httpClient;
            }

            public HttpClient CreateClient(X509Certificate2 clientCertificate) => httpClient;
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode statusCode;
            private readonly string responseBody;

            public Uri RequestUri { get; private set; }
            public string RequestBody { get; private set; }

            public StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
            {
                this.statusCode = statusCode;
                this.responseBody = responseBody;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri;
                RequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody)
                };
            }
        }

        private static X509Certificate2 CreateTestCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=unit-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }
    }
}

