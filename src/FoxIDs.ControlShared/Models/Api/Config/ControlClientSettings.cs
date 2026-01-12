namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Client-side configuration exposed to the Control UI.
    /// </summary>
    public class ControlClientSettings
    {
        /// <summary>
        /// Base FoxIDs endpoint URL.
        /// </summary>
        public string FoxIDsEndpoint { get; set; }

        /// <summary>
        /// Short semantic version.
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Full build version including metadata.
        /// </summary>
        public string FullVersion { get; set; }

        /// <summary>
        /// Selected logging target.
        /// </summary>
        public LogOptions LogOption { get; set; }
        /// <summary>
        /// Selected key storage target.
        /// </summary>
        public KeyStorageOptions KeyStorageOption { get; set; }

        /// <summary>
        /// Whether users may create new tenants through the UI.
        /// </summary>
        public bool EnableCreateNewTenant { get; set; }

        /// <summary>
        /// Indicates payment support is enabled.
        /// </summary>
        public bool EnablePayment { get; set; }
        /// <summary>
        /// Indicates payment is in test mode.
        /// </summary>
        public bool PaymentTestMode { get; set; }
        /// <summary>
        /// Currency code for payments.
        /// </summary>
        public string Currency { get; set;}
        /// <summary>
        /// Mollie profile identifier when payments are enabled.
        /// </summary>
        public string MollieProfileId { get; set;}

        /// <summary>
        /// UI feature flag configuration.
        /// </summary>
        public ControlClientUiSettings ClientUi { get; set; }

        /// <summary>
        /// Module asset URLs used by the Control UI.
        /// </summary>
        public ModuleAssetsSettings ModuleAssets { get; set; }
    }
}
