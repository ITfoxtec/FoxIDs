using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum UsageLogSendTypes
    {
        [EnumMember(Value = "sms")]
        Sms = 20,
        [EnumMember(Value = "email")]
        Email = 30,
    }
}
