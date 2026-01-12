using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    /// <summary>
    /// Authentication method module type.
    /// </summary>
    public enum UpPartyModuleTypes
    {
        /// <summary>
        /// NemLog-in template.
        /// </summary>
        [EnumMember(Value = "nemlogin")]
        NemLogin = 100
    }
}
