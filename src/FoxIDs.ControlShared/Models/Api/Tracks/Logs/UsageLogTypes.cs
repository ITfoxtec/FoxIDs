using System;

namespace FoxIDs.Models.Api
{
    public enum UsageLogTypes
    {
        Hour = 10,
        Day = 20,
        Tenant = 100,
        Track = 120,
        User = 200,
        Login = 300,
        TokenRequest = 400,
        ControlApiGet = 500,
        ControlApiUpdate = 520,
        Passwordless = 580,
        Confirmation = 600,
        SetPassword = 700,   
        [Obsolete("Delete after 2026-06-01 and in combination with FoxIDs.Models.UsageLogTypes.ResetPassword.")]
        ResetPassword = 701,
        Mfa = 800,
        RealCount = 10100,
        ExtraCount = 10120,
        Sms = 10200,
        SmsPrice = 10210,
        Email = 10320,
    }
}
