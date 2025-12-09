namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Dynamic page elements that can be composed in login flows.
    /// </summary>
    public enum DynamicElementTypes
    {
        Email = 5,
        Phone = 6,
        Username = 7,

        EmailAndPassword = 10,
        Password = 11,

        Name = 20,
        GivenName = 21,
        FamilyName = 22,

        Text = 200,
        Html = 210,
        LargeText = 220,
        LargeHtml = 230,

        Checkbox = 300,

        Custom = 1000,

        LoginInput = 2010,
        LoginButton = 2020,
        LoginLink = 2030,
        LoginHrd = 2040
    }
}
