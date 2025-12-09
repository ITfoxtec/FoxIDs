namespace FoxIDs.Models.Api
{
    /// <summary>
    /// UI feature flags returned to the Control client.
    /// </summary>
    public class ControlClientUiSettings
    {
        /// <summary>
        /// Hide branding configuration from the UI.
        /// </summary>
        public bool HideBrandingSettings { get; set; }
        /// <summary>
        /// Hide SMS configuration from the UI.
        /// </summary>
        public bool HideSmsSettings { get; set; }
        /// <summary>
        /// Hide mail configuration from the UI.
        /// </summary>
        public bool HideMailSettings { get; set; }
    }
}
