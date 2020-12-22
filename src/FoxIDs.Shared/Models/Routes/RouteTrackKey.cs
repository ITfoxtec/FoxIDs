namespace FoxIDs.Models
{
    public class RouteTrackKey
    {
        public TrackKeyType Type { get; set; }

        public string ExternalName { get; set; }

        public RouteTrackKeyItem PrimaryKey { get; set; }

        public RouteTrackKeyItem SecondaryKey { get; set; }
    }
}
