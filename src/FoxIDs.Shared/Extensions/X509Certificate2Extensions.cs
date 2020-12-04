using ITfoxtec.Identity;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FoxIDs
{
    /// <summary>
    /// Extension methods for X509Certificate2.
    /// </summary>
    public static class X509Certificate2Extensions
    {
        public static Task<X509Certificate2> CreateSelfSignedCertificateByCnAsync(this string cn)
        {
            return $"CN={cn}, O=FoxIDs".CreateSelfSignedCertificateAsync();
        }
    }
}
