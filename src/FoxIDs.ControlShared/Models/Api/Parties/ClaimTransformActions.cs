namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Actions available when transforming claims.
    /// </summary>
    public enum ClaimTransformActions
    {
        If = 4,
        IfNot = 6,
        Add = 10,
        AddIfNot = 12,
        AddIfNotOut = 15,
        Replace = 20,
        ReplaceIfNot = 22,
        Remove = 30,
    }
}
