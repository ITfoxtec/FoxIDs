using System;

namespace FoxIDs.Models
{
    public enum UsageLogTypes
    {
        Login = 20,
        TokenRequest = 30,
        ControlApiGet = 40,
        ControlApiUpdate = 50,
        Passwordless = 80,
        Confirmation = 100,
        SetPassword = 200,
        [Obsolete("Delete after 2026-06-01 and in combination with FoxIDs.Models.Api.UsageLogTypes.ResetPassword.")]
        ResetPassword = 201,
        Mfa = 300
    }
}
