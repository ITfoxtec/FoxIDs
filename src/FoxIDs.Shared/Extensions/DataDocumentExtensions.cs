using FoxIDs.Models;

namespace FoxIDs
{
    public static class DataDocumentExtensions
    {
        public static void SetPartitionId(this IDataDocument document)
        {
            document.PartitionId = IdToPartitionId(document.Id);
        }

        public static string IdToPartitionId(this string id)
        {
            var idList = id.Split(':');
            if (id.StartsWith("tenant:"))
            {
                return Tenant.PartitionIdFormat();
            }
            else if(id.StartsWith("track:"))
            {
                return Track.PartitionIdFormat(new Track.IdKey { TenantName = idList[1], TrackName = idList[2] });
            }
            else if (id.StartsWith("party:"))
            {
                return DataDocument.PartitionIdFormat(new Track.IdKey { TenantName = idList[2], TrackName = idList[3] });
            }
            else
            {
                return DataDocument.PartitionIdFormat(new Track.IdKey { TenantName = idList[1], TrackName = idList[2] });
            }
        }
    }
}
