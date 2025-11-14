using FoxIDs.Logic.Seed;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FoxIDs.UnitTests.Logic.Seed
{
    public class DataProtectionSeedLogicTests
    {
        [Fact]
        public async Task EnsureKeysAsync_ProtectsDataUsingProvider()
        {
            var protector = new Mock<IDataProtector>();
            protector.Setup(p => p.Protect(nameof(DataProtectionSeedLogic))).Returns(nameof(DataProtectionSeedLogic));

            var provider = new Mock<IDataProtectionProvider>();
            provider.Setup(p => p.CreateProtector(nameof(DataProtectionSeedLogic))).Returns(protector.Object);

            var logic = new DataProtectionSeedLogic(provider.Object);

            await logic.EnsureKeysAsync(CancellationToken.None);

            provider.Verify(p => p.CreateProtector(nameof(DataProtectionSeedLogic)), Times.Once);
            protector.Verify(p => p.Protect(nameof(DataProtectionSeedLogic)), Times.Once);
        }
    }
}
