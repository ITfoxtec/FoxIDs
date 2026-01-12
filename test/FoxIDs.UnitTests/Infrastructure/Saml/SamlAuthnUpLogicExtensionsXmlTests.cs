using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FoxIDs.Logic;
using Xunit;

namespace FoxIDs.UnitTests.Infrastructure.Saml
{
    public class SamlAuthnUpLogicExtensionsXmlTests
    {
        [Fact]
        public void ParseAuthnRequestExtensionsXml_MultipleRootElements_ReturnsElements()
        {
            var extensionsXml = @"
<oiosaml:RequestedAttributeProfiles xmlns:oiosaml=""https://data.gov.dk/eid/saml/extensions"">
  <oiosaml:Profile>https://data.gov.dk/eid/Professional/DK</oiosaml:Profile>
</oiosaml:RequestedAttributeProfiles>
<nl:AppSwitch xmlns:nl=""https://data.gov.dk/eid/saml/extensions"">
  <nl:Platform>Android</nl:Platform>
  <nl:ReturnURL>dk.serviceprovider.test</nl:ReturnURL>
</nl:AppSwitch>";

            var result = InvokeParseAuthnRequestExtensionsXml(extensionsXml);

            Assert.Equal(2, result.Count);
            Assert.Equal("RequestedAttributeProfiles", result.ElementAt(0).Name.LocalName);
            Assert.Equal("AppSwitch", result.ElementAt(1).Name.LocalName);
        }

        private static IReadOnlyCollection<XElement> InvokeParseAuthnRequestExtensionsXml(string authnRequestExtensionsXml)
        {
            var method = typeof(SamlAuthnUpLogic).GetMethod("ParseAuthnRequestExtensionsXml", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            return (IReadOnlyCollection<XElement>)method.Invoke(null, new object[] { authnRequestExtensionsXml });
        }
    }
}

