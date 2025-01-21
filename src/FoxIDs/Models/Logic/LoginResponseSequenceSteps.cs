namespace FoxIDs.Models.Logic
{   
    public enum LoginResponseSequenceSteps
    {
        PhoneVerificationStep = 10,
        EmailVerificationStep = 20,
        MfaSmsStep = 30,
        MfaEmailStep = 31,
        MfaRegisterAuthAppStep = 32,
        MfaAllAndAppStep = 35,
        LoginResponseStep = 40
    }
}
