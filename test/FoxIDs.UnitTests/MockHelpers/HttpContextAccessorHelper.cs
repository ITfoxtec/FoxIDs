using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;

namespace FoxIDs.UnitTests.MockHelpers
{
    public static class HttpContextAccessorHelper
    {
        public static IHttpContextAccessor MockObject(RouteBinding routeBinding)
        {
            var items = new Dictionary<object, object>();
            var cookies = new Dictionary<string, string>();
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.SetupProperty(c => c.Items, items);

            routeBinding.TenantName = routeBinding.TenantName ?? "testtenant";
            routeBinding.TrackName = routeBinding.TrackName ?? "testtrack";
            items.Add(Constants.Routes.RouteBindingKey, routeBinding);
            items.Add(Constants.Routes.SequenceStringKey, "1234asdf1234");

            var mockHttpRequest = new Mock<HttpRequest>();
            mockHttpContext.Setup(c => c.Request).Returns(mockHttpRequest.Object);

            mockHttpRequest.Setup(r => r.Scheme).Returns("https");
            mockHttpRequest.Setup(r => r.Host).Returns(new HostString("foxidstest.test"));
            mockHttpRequest.Setup(r => r.Path).Returns(new PathString("/sometenant/sometrack/(login)/login/CreateUser/_CfDJ8JOhOhitapNOk0YwRh-azW-uLvIVSVeabekwNFrDKn836MraokC1PKD-7HatR09hT5CwAjOV7L5QkNyBP11yExMbePpBDAg8ohpk7TxflZ-EOV7Ib6T4rRwYqzORMrNF9zty3y0wsgmgSYP9njaPvMpfA3W3sKZIWq5S_IKdbKNs"));
            var requestHeaders = new HeaderDictionary { ["User-Agent"] = "unit-test-agent" };
            mockHttpRequest.Setup(r => r.Headers).Returns(requestHeaders);

            var mockRequestCookieCollection = new Mock<IRequestCookieCollection>();
            mockHttpRequest.Setup(r => r.Cookies).Returns(mockRequestCookieCollection.Object);
            mockRequestCookieCollection.Setup(rc => rc[It.IsAny<string>()]).Returns((string key) => cookies[key]);
            mockRequestCookieCollection.Setup(rc => rc.Keys).Returns(cookies.Keys);

            var mockHttpResponse = new Mock<HttpResponse>();
            mockHttpContext.Setup(c => c.Response).Returns(mockHttpResponse.Object);
            var mockResponseCookies = new Mock<IResponseCookies>();
            mockHttpResponse.Setup(r => r.Cookies).Returns(mockResponseCookies.Object);
            mockResponseCookies.Setup(rc => rc.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>())).Callback((string key, string value, CookieOptions options) => cookies[key] = value);

            var mockConnectionInfo = new Mock<ConnectionInfo>();
            mockConnectionInfo.Setup(c => c.RemoteIpAddress).Returns(IPAddress.Parse("127.0.0.1"));
            mockHttpContext.Setup(c => c.Connection).Returns(mockConnectionInfo.Object);

            var mockIServiceProvider = new Mock<IServiceProvider>();
            mockHttpContext.Setup(c => c.RequestServices).Returns(mockIServiceProvider.Object);

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(ca => ca.HttpContext).Returns(mockHttpContext.Object);

            return mockHttpContextAccessor.Object;
        }
    }
}
