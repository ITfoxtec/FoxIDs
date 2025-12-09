using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Adds optional extended UI definitions to a request or response.
    /// </summary>
    public interface IExtendedUisRef
    {
        /// <summary>
        /// UI customizations scoped to the party or client.
        /// </summary>
        public List<ExtendedUi> ExtendedUis { get; set; }
    }
}
