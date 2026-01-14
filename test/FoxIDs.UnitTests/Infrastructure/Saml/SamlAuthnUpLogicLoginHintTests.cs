using System;
using System.Reflection;
using System.Web;
using FoxIDs.Logic;
using Xunit;

namespace FoxIDs.UnitTests.Infrastructure.Saml
{
    public class SamlAuthnUpLogicLoginHintTests
    {
        [Fact]
        public void AddLoginHintQueryParameter_WithExistingQuery_SetsLoginHint()
        {
            var destination = new Uri("https://login.microsoftonline.com/tenant/saml2?foo=bar");

            var result = InvokeAddLoginHintQueryParameter(destination, "user@example.com");

            var query = HttpUtility.ParseQueryString(result.Query);
            Assert.Equal("bar", query["foo"]);
            Assert.Equal("user@example.com", query["login_hint"]);
        }

        [Fact]
        public void AddLoginHintQueryParameter_ExistingLoginHint_ReplacesValue()
        {
            var destination = new Uri("https://login.microsoftonline.com/tenant/saml2?login_hint=old@example.com");

            var result = InvokeAddLoginHintQueryParameter(destination, "new@example.com");

            var query = HttpUtility.ParseQueryString(result.Query);
            Assert.Equal("new@example.com", query["login_hint"]);
        }

        private static Uri InvokeAddLoginHintQueryParameter(Uri destination, string loginHint)
        {
            var method = typeof(SamlAuthnUpLogic).GetMethod("AddLoginHintQueryParameter", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            return (Uri)method.Invoke(null, new object[] { destination, loginHint });
        }
    }
}
