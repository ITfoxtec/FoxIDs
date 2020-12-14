using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;

namespace FoxIDs
{
    public static class TrackKeyExtensions
    {
        public static JsonWebKey GetPublicKey(this TrackKeyItem trackKeyItem)
        {
            return trackKeyItem.Key.GetPublicKey();
        }
    }
}
