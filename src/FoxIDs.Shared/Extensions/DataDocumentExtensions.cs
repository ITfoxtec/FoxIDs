using FoxIDs.Models;
using ITfoxtec.Identity;

namespace FoxIDs
{
    public static class DataDocumentExtensions
    {
        public static void SetDataType(this IDataDocument document)
        {
            if(document is DataElement && (document as DataElement).DataType.IsNullOrEmpty())
            {
                if (document.Id.StartsWith("party:"))
                {
                    var idList = document.Id.Split(':');
                    (document as DataElement).DataType = $"{idList[0]}:{idList[1]}";
                }
                else
                {
                    (document as DataElement).DataType = document.Id.Substring(0, document.Id.IndexOf(':'));
                }
            }
        }

        public static string IdToMasterPartitionId(this string id)
        {
            if (id.StartsWith("prisk:"))
            {
                return RiskPassword.PartitionIdFormat(new MasterDocument.IdKey());
            }
            else
            {
                return MasterDocument.PartitionIdFormat(new MasterDocument.IdKey());
            }
        }

        public static void SetTenantPartitionId(this IDataDocument document)
        {
            document.PartitionId = IdToTenantPartitionId(document.Id);
        }

        public static string IdToTenantPartitionId(this string id)
        {
            var idList = id.Split(':');
            if (id.StartsWith("tenant:"))
            {
                return Tenant.PartitionIdFormat();
            }
            else if (id.StartsWith("track:"))
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
