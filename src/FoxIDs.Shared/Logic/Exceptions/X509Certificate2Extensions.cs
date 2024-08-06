using FoxIDs.Models;
using ITfoxtec.Identity;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public static class X509Certificate2Extensions
    {
        public static async Task<TrackKeyItem> ToTrackKeyItemAsync(this CertificateItem certificateItem, bool includePrivateKey = false)
        {
            return new TrackKeyItem
            {
                Key = await certificateItem.Certificate.ToFTJsonWebKeyAsync(includePrivateKey),
                NotBefore = certificateItem.NotBefore,
                NotAfter = certificateItem.NotAfter
            };
        }
    }
}
