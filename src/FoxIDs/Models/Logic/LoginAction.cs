using System.Runtime.Serialization;

namespace FoxIDs.Models.Logic
{
    public enum LoginAction
    {
        [EnumMember(Value = "read_session")]
        ReadSession,
        [EnumMember(Value = "read_session_or_login")]
        ReadSessionOrLogin,     
        [EnumMember(Value = "session_user_require_login")]
        SessionUserRequireLogin,
        [EnumMember(Value = "require_login")]
        RequireLogin,
    }
}
