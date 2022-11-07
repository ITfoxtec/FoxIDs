using ITfoxtec.Identity;

namespace FoxIDs.Client
{
    public static class LogExtensions
    {
        public static string RemovePreFLogKey(this string logKey)
        {
            if (!logKey.IsNullOrEmpty() && logKey.StartsWith("f_"))
            {
                return logKey.Substring(2);
            }

            return logKey;
        }
    }
}
