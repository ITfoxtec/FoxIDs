namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Key storage choices for managing signing keys.
    /// </summary>
    public enum KeyStorageOptions
    {
        /// <summary>
        /// No external key storage configured.
        /// </summary>
        None = 100,
        /// <summary>
        /// Store keys in Azure Key Vault.
        /// </summary>
        KeyVault = 1100,
    }
}
