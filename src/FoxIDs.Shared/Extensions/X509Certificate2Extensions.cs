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
            return $"CN={subjectData.tenantName}{(subjectData.trackName.Equals("-", StringComparison.Ordinal) ? string.Empty : $"-{subjectData.trackName}")}, O=FoxIDs";
        }

        public static Task<X509Certificate2> CreateSelfSignedCertificateBySubjectAsync(this RouteBinding routeBinding)
        {
            return (routeBinding.TenantName, routeBinding.TrackName).CreateSelfSignedCertificateBySubjectAsync();
        }

        public static Task<X509Certificate2> CreateSelfSignedCertificateBySubjectAsync(this (string tenantName, string trackName) subjectData)
        {
            var subject = (subjectData.tenantName, subjectData.trackName).GetCertificateSubject();
            return subject.CreateSelfSignedCertificateAsync();
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
