namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Execution points where claim transforms can run.
    /// </summary>
    public enum ClaimTransformTasks
    {        
        RequestException = 20,
        UpPartyAction = 100,
        QueryInternalUser = 200,
        QueryExternalUser = 220,
        SaveClaimInternalUser = 300,
        SaveClaimExternalUser = 320,
        LogEvent = 400,
    }
}