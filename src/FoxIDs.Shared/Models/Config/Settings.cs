﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class Settings
    {
        /// <summary>
        /// FoxIDs Control endpoint, used in FoxIDs Control.
        /// </summary>
        public string FoxIDsControlEndpoint { get; set; }

        /// <summary>
        /// Cosmos DB configuration.
        /// </summary>
        [Required]
        public CosmosDbSettings CosmosDb { get; set; }

        /// <summary>
        /// Key Vault configuration.
        /// </summary>
        [Required]
        public KeyVaultSettings KeyVault { get; set; }

        /// <summary>
        /// Enable master seed if true.
        /// </summary>
        public bool MasterSeedEnabled { get; set; }

        /// <summary>
        /// Only used in development!
        /// The servers client credentials. 
        /// </summary>
        public ClientCredentialSettings ServerClientCredential { get; set; }
    }
}
