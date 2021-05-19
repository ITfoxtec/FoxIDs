using System;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for X509Certificate2.
    /// </summary>
    public static class X509Certificate2Extensions
    {
        public static bool IsValid(this X509Certificate2 certificate, DateTime nowLocal)
        {
            if (certificate.NotBefore <= nowLocal && certificate.NotAfter >= nowLocal)
            {
                return true;
            }

            return false;
        }
    }
}
