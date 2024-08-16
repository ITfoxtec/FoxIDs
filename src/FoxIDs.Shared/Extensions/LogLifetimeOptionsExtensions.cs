using FoxIDs.Models;
using System;

namespace FoxIDs
{
    public static class LogLifetimeOptionsExtensions
    {
        public static int GetLifetimeInDays(this LogLifetimeOptions logLifetime)
        {
            return logLifetime switch
            {
                LogLifetimeOptions.Max30Days => 30,
                LogLifetimeOptions.Max180Days => 180,
                _ => throw new NotSupportedException(),
            };
        }
    }
}
