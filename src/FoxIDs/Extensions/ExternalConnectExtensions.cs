using FoxIDs.Models.External;

namespace FoxIDs
{
    public static class ExternalConnectExtensions
    {
        public static string GetErrorMessage(this ErrorResponse errorResponse)
        {
            return string.IsNullOrWhiteSpace(errorResponse.ErrorMessage) ? string.Empty : $" {errorResponse.ErrorMessage.TrimEnd('.')}.";
        }
    }
}
