using System.Collections.Generic;
using FoxIDs.Infrastructure.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace FoxIDs.UnitTests.Infrastructure.Saml
{
    public class SamlRequestHelperTests
    {
        [Fact]
        public void IsAuthnMetadataRequest_GetWithoutSamlRequest_ReturnsTrue()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;

            var result = SamlRequestHelper.IsAuthnMetadataRequest(context.Request);

            Assert.True(result);
        }

        [Fact]
        public void IsAuthnMetadataRequest_GetWithQuerySamlRequest_ReturnsFalse()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.QueryString = new QueryString("?SAMLRequest=value");

            var result = SamlRequestHelper.IsAuthnMetadataRequest(context.Request);

            Assert.False(result);
        }

        [Fact]
        public void IsAuthnMetadataRequest_GetWithFormSamlRequest_ReturnsFalse()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.ContentType = "application/x-www-form-urlencoded";
            var form = new FormCollection(new Dictionary<string, StringValues> { [Saml2Constants.Message.SamlRequest] = "value" });
            context.Features.Set<IFormFeature>(new FormFeature(form));

            var result = SamlRequestHelper.IsAuthnMetadataRequest(context.Request);

            Assert.False(result);
        }

        [Fact]
        public void IsAuthnMetadataRequest_PostWithoutSamlRequest_ReturnsFalse()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;

            var result = SamlRequestHelper.IsAuthnMetadataRequest(context.Request);

            Assert.False(result);
        }
    }
}
