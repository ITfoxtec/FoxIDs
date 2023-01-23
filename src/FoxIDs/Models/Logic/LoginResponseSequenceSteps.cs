namespace FoxIDs.Models.Logic
{   
    public enum LoginResponseSequenceSteps
    {
        FromEmailVerificationStep = 1, 
        FromMfaStep = 2,
        FromLoginResponseStep = 3
    }
}
