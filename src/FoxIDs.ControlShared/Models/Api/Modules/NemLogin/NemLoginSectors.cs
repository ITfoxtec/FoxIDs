namespace FoxIDs.Models.Api
{
    /// <summary>
    /// NemLog-in sector selection.
    /// </summary>
    public enum NemLoginSectors
    {
        /// <summary>
        /// Public sector (OIOSAML 3.0.3).
        /// </summary>
        PublicOiosaml303 = 1100,

        /// <summary>
        /// Public sector (OIOSAML 4.0.0).
        /// </summary>
        PublicOiosaml400 = 1110,

        /// <summary>
        /// Private sector (OIOSAML 3.0.3).
        /// </summary>
        PrivateOiosaml303 = 1200,

        /// <summary>
        /// Private sector (OIOSAML 4.0.0).
        /// </summary>
        PrivateOiosaml400 = 1210
    }
}
