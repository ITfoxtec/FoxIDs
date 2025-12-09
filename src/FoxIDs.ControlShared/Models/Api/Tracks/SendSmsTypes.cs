namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Supported SMS providers.
    /// </summary>
    public enum SendSmsTypes
    {
        GatewayApi = 100,
        Smstools = 200,
        TeliaSmsGateway = 300,
    }
}
