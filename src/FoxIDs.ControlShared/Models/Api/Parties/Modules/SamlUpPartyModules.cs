using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// SAML 2.0 authentication method modules.
    /// </summary>
    public class SamlUpPartyModules
    {
        /// <summary>
        /// If enabled, the standard SAML 2.0 settings view is shown instead of the modules template view.
        /// </summary>
        public bool ShowStandardSettings { get; set; }

        /// <summary>
        /// NemLog-in module configuration.
        /// </summary>
        [ValidateComplexType]
        public SamlUpPartyNemLoginModule NemLogin { get; set; }

    }
}