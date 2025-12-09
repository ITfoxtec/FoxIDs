namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Transformation strategies supported when issuing claims.
    /// </summary>
    public enum ClaimTransformTypes
    {
        Constant = 10,
        MatchClaim = 17,
        Match = 20,
        RegexMatch = 25,
        Map = 30,
        RegexMap = 35,
        Concatenate = 40,
        ExternalClaims = 50,
        DkPrivilege = 1010
    }
}
