using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class FoxIDsControlHttpSecurityHeadersAttributeTests
    {
        [Fact]
        public void ApplyFromMiddleware_AddsFoxIDsEndpointToConnectSrc()
        {
            var settings = new FoxIDsControlSettings
            {
                FoxIDsEndpoint = "https://foxids.example.com",
                FoxIDsControlEndpoint = "https://control.example.com"
            };
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

            var securityHeaders = new FoxIDsControlHttpSecurityHeadersAttribute.FoxIDsControlHttpSecurityHeadersActionAttribute(settings, environment.Object);

            var httpContext = new DefaultHttpContext();

            securityHeaders.ApplyFromMiddleware(httpContext, isHtml: true);

            var cspHeader = httpContext.Response.Headers["Content-Security-Policy"].ToString();
            Assert.Contains("connect-src 'self' https://foxids.example.com", cspHeader);
        }

        [Fact]
        public void ApplyFromMiddleware_AllowsDownloadsInSandbox()
        {
            var settings = new FoxIDsControlSettings
            {
                FoxIDsEndpoint = "https://foxids.example.com",
                FoxIDsControlEndpoint = "https://control.example.com"
            };
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

            var securityHeaders = new FoxIDsControlHttpSecurityHeadersAttribute.FoxIDsControlHttpSecurityHeadersActionAttribute(settings, environment.Object);

            var httpContext = new DefaultHttpContext();

            securityHeaders.ApplyFromMiddleware(httpContext, isHtml: true);

            var cspHeader = httpContext.Response.Headers["Content-Security-Policy"].ToString();
            Assert.Contains("sandbox allow-forms allow-popups allow-same-origin allow-scripts allow-downloads;", cspHeader);
        }
    }
}
