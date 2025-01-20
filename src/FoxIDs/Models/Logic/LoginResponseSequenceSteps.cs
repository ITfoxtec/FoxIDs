namespace FoxIDs.Models.Logic
{   
    public enum LoginResponseSequenceSteps
    {
        FromPhoneVerificationStep = 10,
        FromEmailVerificationStep = 20, 
        FromMfaSmsStep = 31,
        FromMfaEmailStep = 31,
        FromMfaAllAndAppStep = 35,
        FromLoginResponseStep = 40
    }
}
