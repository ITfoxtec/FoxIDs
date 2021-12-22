using ITfoxtec.Identity.Models;

namespace FoxIDs.Models
{
    public class RouteTrackKeyItem
    {
        public JsonWebKey Key { get; set; }

        public string ExternalId { get; set; }

        public bool ExternalKeyIsNotReady { get; set; }
    }
}
