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
                return idList[1];
            }
            else if (id.StartsWith("party:"))
            {

                return $"{idList[2]}:{idList[3]}";
            }
            else
            {

                return $"{idList[1]}:{idList[2]}";
            }
        }
    }
}
