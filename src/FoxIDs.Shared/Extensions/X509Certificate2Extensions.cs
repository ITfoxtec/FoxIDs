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
            return subject.CreateSelfSignedCertificateAsync(expiry: TimeSpan.FromDays(365.0 * 3));
        }
    }
}
