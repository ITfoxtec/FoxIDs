using System;
using System.Net.Http;
using System.Threading.Tasks;
using FoxIDs.Logic;
using FoxIDs.Models;
using Moq;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class SamlMetadataReadLogicTests
    {
        [Fact]
        public async Task PopulateModelAsync_WithSpMetadata_SetsIssuerBindingsAndUrls()
        {
            var logic = new SamlMetadataReadLogic(new Mock<IHttpClientFactory>().Object);
            var party = new SamlDownParty();
            var metadataXml = CreateSpMetadataXml();

            var result = await logic.PopulateModelAsync(party, metadataXml);

            Assert.Equal("https://sp.example.com/metadata", result.Issuer);
            Assert.Equal(2, result.AcsUrls.Count);
            Assert.Contains("https://sp.example.com/acs", result.AcsUrls);
            Assert.Contains("https://sp.example.com/acs2", result.AcsUrls);
            Assert.NotNull(result.AuthnBinding);
            Assert.Equal(SamlBindingTypes.Post, result.AuthnBinding.RequestBinding);
            Assert.Equal(SamlBindingTypes.Post, result.AuthnBinding.ResponseBinding);
            Assert.Equal("https://sp.example.com/logout", result.SingleLogoutUrl);
            Assert.NotNull(result.LogoutBinding);
            Assert.Equal(SamlBindingTypes.Redirect, result.LogoutBinding.RequestBinding);
            Assert.Equal(SamlBindingTypes.Redirect, result.LogoutBinding.ResponseBinding);
            Assert.Null(result.Keys);
            Assert.False(result.EncryptAuthnResponse);
            Assert.True(result.LastUpdated > 0);
        }

        private static string CreateSpMetadataXml()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<EntityDescriptor entityID=""https://sp.example.com/metadata"" xmlns=""urn:oasis:names:tc:SAML:2.0:metadata"">
  <SPSSODescriptor protocolSupportEnumeration=""urn:oasis:names:tc:SAML:2.0:protocol"">
    <AssertionConsumerService Binding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST"" Location=""https://sp.example.com/acs"" index=""0"" isDefault=""true"" />
    <AssertionConsumerService Binding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect"" Location=""https://sp.example.com/acs2"" index=""1"" />
    <SingleLogoutService Binding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect"" Location=""https://sp.example.com/logout"" />
  </SPSSODescriptor>
</EntityDescriptor>";
        }
    }
}
