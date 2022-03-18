using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace FoxIDs.Infrastructure.KeyVault
{
    public static class FoxIDsKeyVaultClient
    {
        public static KeyVaultClient GetManagedClient()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            return client;
        }
    }
}
