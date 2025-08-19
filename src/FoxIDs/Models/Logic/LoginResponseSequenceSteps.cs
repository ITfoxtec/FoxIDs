namespace FoxIDs.Models.Logic
{   
    public enum LoginResponseSequenceSteps
    {
        PhoneVerificationStep = 10,
        EmailVerificationStep = 20,
        MfaRegisterAuthAppStep = 32,
        MfaAllAndAppStep = 35,
        LoginResponseStep = 40
    }
}
