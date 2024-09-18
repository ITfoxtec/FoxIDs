using ITfoxtec.Identity;

namespace FoxIDs.Client
{
    public static class LogExtensions
    {
        public static string GetDisplayLogKey(this string logKey)
        {
            if (!logKey.IsNullOrEmpty())
            {
                return logKey.Replace("Track", "Environment").Replace("DownParty", "Application").Replace("UpParty", "AuthMethod");
            }

            return logKey;
        }

        public static string GetDisplayLogValue(this string logValue)
        {
            if (!logValue.IsNullOrEmpty())
            {
                return logValue.Replace("party:down:", string.Empty).Replace("party:up:", string.Empty);
            }

            return logValue;
        }

        
    }
}
