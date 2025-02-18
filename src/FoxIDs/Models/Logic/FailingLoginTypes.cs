namespace FoxIDs.Models.Logic
{
    public enum FailingLoginTypes
    {
        Login,
        ExternalLogin,
        SmsCode,
        EmailCode,
        TwoFactorSmsCode,
        TwoFactorEmailCode,
        TwoFactorAuthenticator,
    }
}
