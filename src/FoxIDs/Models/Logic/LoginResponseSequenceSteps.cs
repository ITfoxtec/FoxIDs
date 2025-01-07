namespace FoxIDs.Models.Logic
{   
    public enum LoginResponseSequenceSteps
    {
        FromPhoneVerificationStep = 10,
        FromEmailVerificationStep = 20, 
        FromMfaStep = 30,
        FromLoginResponseStep = 40
    }
}
