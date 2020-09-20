using System;
using FoxIDs.Models.Config;
using ITfoxtec.Identity.Helpers;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace FoxIDs.Infrastructure.KeyVault
{
    public static class FoxIDsKeyVaultClient
    {
        //public static KeyVaultClient GetClient(Settings settings, TokenHelper tokenHelper)
        //{
        //    var client = new KeyVaultClient(async (authority, resource, scope) =>
        //    {
        //        try
        //        {
        //            var tokenRequest = new ADTokenRequest
        //            {
        //                Resource = resource
        //            };
        //            (var accessToken, var expiresIn) = await tokenHelper.GetAccessTokenWithClientCredentialsAsync(settings.KeyVault.ClientId, settings.KeyVault.ClientSecret, $"{authority}/oauth2/token", tokenRequest);
        //            return accessToken;
        //        }
        //        catch (Exception ex)
        //        {
        //            throw new Exception("Error while retrieving a token from Azure AD to Azure Key Vault.", ex);
        //        }
        //    });

        //    return client;
        //}

        public static KeyVaultClient GetManagedClient()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            return client;
        }
    }
}
