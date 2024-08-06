using FoxIDs.Logic;
using FoxIDs.Models;
using ITfoxtec.Identity;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for X509Certificate2.
    /// </summary>
    public static class X509Certificate2Extensions
    {
        public static string GetCertificateSubject(this (string tenantName, string trackName) subjectData)
        {
            return $"CN=FoxIDs - {subjectData.tenantName}{(subjectData.trackName.Equals("-", StringComparison.Ordinal) ? string.Empty : $"-{subjectData.trackName}")}, O=FoxIDs";
        }

        public static Task<CertificateItem> CreateSelfSignedCertificateBySubjectAsync(this RouteBinding routeBinding, int expiryInMonths = 36)
        {
            return (routeBinding.TenantName, routeBinding.TrackName).CreateSelfSignedCertificateBySubjectAsync(expiryInMonths);
        }

        public static async Task<CertificateItem> CreateSelfSignedCertificateBySubjectAsync(this (string tenantName, string trackName) subjectData, int expiryInMonths = 36)
        {
            var subject = (subjectData.tenantName, subjectData.trackName).GetCertificateSubject();
            var now = DateTimeOffset.UtcNow;
            var notBefore = now.AddSeconds(-5);
            var notAfter = now.AddMonths(expiryInMonths);
            return new CertificateItem
            {
                Certificate = await subject.CreateSelfSignedCertificateAsync(notBefore, notAfter),
                NotBefore = notBefore.ToUnixTimeSeconds(),
                NotAfter = notAfter.ToUnixTimeSeconds()
            };
        }

        public static bool IsValidateCertificate(this X509Certificate2 certificate)
        {
            var nowLocal = DateTime.Now;
            if (certificate.NotBefore > nowLocal)
            {
                return false;
            }
            if (certificate.NotAfter < nowLocal)
            {
                return false;
            }
            return true;
        }

        public static void ValidateCertificate(this X509Certificate2 certificate, string postErrorMessage)
        {
            var nowLocal = DateTime.Now;
            if (certificate.NotBefore > nowLocal)
            {
                throw new KeyException($"{postErrorMessage} certificate not valid yet. Not before {certificate.NotBefore.ToUniversalTime():u}.");
            }
            if (certificate.NotAfter < nowLocal)
            {
                throw new KeyException($"{postErrorMessage} certificate has expired. Not after {certificate.NotAfter.ToUniversalTime():u}.");
            }
        }
    }
}
