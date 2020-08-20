using System;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.Client
{
    public static class CertificateExtensions
    {
        public static bool IsValid(this X509Certificate2 certificate)
        {
            var localTime = DateTime.Now;
            if (certificate.NotBefore > localTime) return false;
            if (certificate.NotAfter < localTime) return false;
            return true;
        }
    }
}
