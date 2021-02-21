using System;
using System.Collections.Generic;

namespace FoxIDs
{
    public static class ExceptionExtensions
    {
        public static string AllMessagesJoined(this Exception ex)
        {
            return string.Join("; ", ex.AllMessages());
        }

        public static List<string> AllMessages(this Exception ex)
        {
            var messages = new List<string>();
            return AllMessages(messages, ex);
        }

        private static List<string> AllMessages(List<string> messages, Exception ex)
        {
            if (ex != null)
            {
                messages.Add(ex.Message);
                if (ex.InnerException != null)
                {
                    return AllMessages(messages, ex.InnerException);
                }
            }
            return messages;
        }
    }
}
