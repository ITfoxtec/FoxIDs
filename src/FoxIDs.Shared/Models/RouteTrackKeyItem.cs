using Microsoft.IdentityModel.Tokens;

namespace FoxIDs.Models
{
    public class RouteTrackKeyItem
    {
        public JsonWebKey Key { get; set; }

        public string ExternalId { get; set; }
    }
}
