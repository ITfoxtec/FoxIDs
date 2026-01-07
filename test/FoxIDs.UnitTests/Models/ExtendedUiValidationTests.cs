using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models;
using FoxIDs.Models.Modules;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FoxIDs.UnitTests.Models
{
    public class ExtendedUiValidationTests
    {
        [Fact]
        public async Task Validate_NemLoginPrivateCprMatch_MissingModules_DefaultsIntegrationTest()
        {
            var extendedUi = new ExtendedUi
            {
                Name = "nemlogin_cpr",
                Title = "Enter CPR number",
                SubmitButtonText = "Continue",
                PredefinedType = ExtendedUiPredefinedTypes.NemLoginPrivateCprMatch,
                Modules = new ExtendedUiModules
                {
                    NemLogin = new ExtendedUiNemLoginModule { Environment = NemLoginEnvironments.IntegrationTest }
                },
                Elements = new List<DynamicElement>
                {
                    new DynamicElement { Type = DynamicElementTypes.Text, Order = 0, Content = "Test" }
                }
            };

            await extendedUi.ValidateObjectAsync();

            Assert.NotNull(extendedUi.Modules);
            Assert.NotNull(extendedUi.Modules.NemLogin);
            Assert.Equal(NemLoginEnvironments.IntegrationTest, extendedUi.Modules.NemLogin.Environment);
        }

        [Fact]
        public async Task Validate_NemLoginPrivateCprMatch_ProductionEnvironment_PreservesValue()
        {
            var extendedUi = new ExtendedUi
            {
                Name = "nemlogin_cpr",
                Title = "Enter CPR number",
                SubmitButtonText = "Continue",
                PredefinedType = ExtendedUiPredefinedTypes.NemLoginPrivateCprMatch,
                Modules = new ExtendedUiModules
                {
                    NemLogin = new ExtendedUiNemLoginModule { Environment = NemLoginEnvironments.Production }
                },
                Elements = new List<DynamicElement>
                {
                    new DynamicElement { Type = DynamicElementTypes.Text, Order = 0, Content = "Test" }
                }
            };

            await extendedUi.ValidateObjectAsync();

            Assert.NotNull(extendedUi.Modules);
            Assert.NotNull(extendedUi.Modules.NemLogin);
            Assert.Equal(NemLoginEnvironments.Production, extendedUi.Modules.NemLogin.Environment);
        }

        [Fact]
        public async Task Validate_CustomExtendedUi_MissingTitle_Throws()
        {
            var extendedUi = new ExtendedUi
            {
                Name = "custom",
                Elements = new List<DynamicElement>
                {
                    new DynamicElement { Type = DynamicElementTypes.Text, Order = 1, Content = "Test" }
                }
            };

            await Assert.ThrowsAsync<ValidationResultException>(async () => await extendedUi.ValidateObjectAsync());
        }

        [Fact]
        public async Task Validate_CustomExtendedUi_MissingElements_Throws()
        {
            var extendedUi = new ExtendedUi
            {
                Name = "custom",
                Title = "Title"
            };

            await Assert.ThrowsAsync<ValidationResultException>(async () => await extendedUi.ValidateObjectAsync());
        }

        [Fact]
        public async Task Validate_NemLoginPrivateCprMatch_ClearsNonConfigFields()
        {
            var extendedUi = new ExtendedUi
            {
                Name = "nemlogin_cpr",
                Title = "Should not be stored",
                SubmitButtonText = "Should not be stored",
                PredefinedType = ExtendedUiPredefinedTypes.NemLoginPrivateCprMatch,
                Modules = new ExtendedUiModules
                {
                    NemLogin = new ExtendedUiNemLoginModule { Environment = NemLoginEnvironments.IntegrationTest }
                },
                Elements = new List<DynamicElement>
                {
                    new DynamicElement { Type = DynamicElementTypes.Text, Order = 1, Content = "Should not be stored" }
                }
            };

            await extendedUi.ValidateObjectAsync();

            Assert.Null(extendedUi.Title);
            Assert.Null(extendedUi.SubmitButtonText);
            Assert.Null(extendedUi.Elements);
        }
    }
}
