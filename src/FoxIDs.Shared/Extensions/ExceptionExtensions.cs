using System;
using System.Collections.Generic;

namespace FoxIDs
{
    public static class ExceptionExtensions
    {
        public static string GetAllMessagesJoined(this Exception ex)
        {
            return string.Join("; ", ex.GetAllMessages());
        }

        public static List<string> GetAllMessages(this Exception ex)
        {
            var messages = new List<string>();
            return GetAllMessages(messages, ex);
        }

        private static List<string> GetAllMessages(List<string> messages, Exception ex)
        {
            if (ex != null)
            {
                messages.Add(ex.Message);
                if (ex.InnerException != null)
                {
                    return GetAllMessages(messages, ex.InnerException);
                }
            }
            return messages;
        }
    }
}
