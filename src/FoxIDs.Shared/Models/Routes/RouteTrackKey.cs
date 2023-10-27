namespace FoxIDs.Models
{
    public class RouteTrackKey
    {
        public TrackKeyTypes Type { get; set; }

        public string ExternalName { get; set; }

        public RouteTrackKeyItem PrimaryKey { get; set; }

        public RouteTrackKeyItem SecondaryKey { get; set; }
    }
}
