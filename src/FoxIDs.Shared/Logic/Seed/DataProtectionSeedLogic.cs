using Microsoft.AspNetCore.DataProtection;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Seed
{
    public class DataProtectionSeedLogic
    {
        private readonly IDataProtectionProvider dataProtectionProvider;

        public DataProtectionSeedLogic(IDataProtectionProvider dataProtectionProvider)
        {
            this.dataProtectionProvider = dataProtectionProvider;
        }

        public Task EnsureKeysAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var protector = dataProtectionProvider.CreateProtector(nameof(DataProtectionSeedLogic));
            protector.Protect(nameof(DataProtectionSeedLogic));

            return Task.CompletedTask;
        }
    }
}