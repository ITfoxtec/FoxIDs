using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FoxIDs.Models;
using Xunit;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.UnitTests
{
    public class SamlUpPartyModuleTypeTests
    {
        [Fact]
        public void Validate_MicrosoftEntraIdModuleType_ReturnsNoErrorsForSharedModel()
        {
            var party = new SamlUpParty
            {
                Issuers = new List<string> { "issuer" },
                ModuleType = UpPartyModuleTypes.MicrosoftEntraId
            };

            var results = party.Validate(new ValidationContext(party)).ToList();

            Assert.Empty(results);
        }

        [Fact]
        public void Validate_MicrosoftEntraIdModuleType_ReturnsNoErrorsForApiModel()
        {
            var party = new Api.SamlUpParty
            {
                DisplayName = "Microsoft Entra ID",
                AuthnResponseBinding = Api.SamlBindingTypes.Post,
                MetadataUrl = "https://login.microsoftonline.com/tenant/federationmetadata/2007-06/federationmetadata.xml",
                ModuleType = Api.UpPartyModuleTypes.MicrosoftEntraId,
                Modules = new Api.SamlUpPartyModules()
            };

            var results = party.Validate(new ValidationContext(party)).ToList();

            Assert.Empty(results);
        }
    }
}
