using System.Runtime.Serialization;

namespace FoxIDs.Models.Modules;

/// <summary>
/// NemLog-in sector selection.
/// </summary>
public enum NemLoginSectors
{
    /// <summary>
    /// Public sector (OIOSAML 3.0.3).
    /// </summary>
    [EnumMember(Value = "public_oiosaml303")]
    PublicOiosaml303 = 1100,

    /// <summary>
    /// Public sector (OIOSAML 4.0.0).
    /// </summary>
    [EnumMember(Value = "public_oiosaml400")]
    PublicOiosaml400 = 1110,

    /// <summary>
    /// Private sector (OIOSAML 3.0.3).
    /// </summary>
    [EnumMember(Value = "private_oiosaml303")]
    PrivateOiosaml303 = 1200,

    /// <summary>
    /// Private sector (OIOSAML 4.0.0).
    /// </summary>
    [EnumMember(Value = "private_oiosaml400")]
    PrivateOiosaml400 = 1210
}
