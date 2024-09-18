using ITfoxtec.Identity;
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

        private static List<string> GetAllMessages(List<string> messages, Exception ex, string ignoreMessage = null)
        {
            if (ex != null)
            {
                if (ignoreMessage == null || !(ex.Message?.StartsWith(ignoreMessage) == true))
                {
                    messages.Add(ex.Message);
                    if (ex.InnerException != null)
                    {
                        var ignoreMessageNextLevel = !ex.Message.IsNullOrWhiteSpace() && ex.Message.Length > 50 ? ex.Message.Substring(0, ex.Message.Length / 2) : null;
                        return GetAllMessages(messages, ex.InnerException, ignoreMessageNextLevel);
                    }
                }
            }
            return messages;
        }
    }
}
