﻿namespace FoxIDs.Models
{
    public enum FailingLoginTypes
    {
        InternalLogin = 100,
        ExternalLogin = 120,
        SmsCode = 200,
        EmailCode = 220,
        TwoFactorSmsCode = 300,
        TwoFactorEmailCode = 320,
        TwoFactorAuthenticator = 340
    }
}
