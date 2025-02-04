using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum UsageLogTypes
    {
        [EnumMember(Value = "login")]
        Login = 20,
        [EnumMember(Value = "token_request")]
        TokenRequest = 30,
        [EnumMember(Value = "control_api_get")]
        ControlApiGet = 40,
        [EnumMember(Value = "control_api_update")]
        ControlApiUpdate = 50,
        [EnumMember(Value = "confirmation")]
        Confirmation = 100,
        [EnumMember(Value = "reset_password")]
        ResetPassword = 200,
        [EnumMember(Value = "mfa")]
        Mfa = 300
    }
}
