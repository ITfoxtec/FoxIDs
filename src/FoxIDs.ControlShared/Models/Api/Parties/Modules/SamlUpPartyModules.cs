using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// SAML 2.0 authentication method modules.
    /// </summary>
    public class SamlUpPartyModules
    {
        /// <summary>
        /// NemLog-in module configuration.
        /// </summary>
        [ValidateComplexType]
        public SamlUpPartyNemLoginModule NemLogin { get; set; }
    }
}

