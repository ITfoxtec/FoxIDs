using Microsoft.Azure.Cosmos;
using System;
using System.Net;

namespace FoxIDs.Repository
{
    [Serializable]
    public class FoxIDsDataException : Exception
    {
        public FoxIDsDataException() { }
        public FoxIDsDataException(string partitionId) : base(IdToMessage(partitionId)) { }
        public FoxIDsDataException(string id, string partitionId) : base(IdToMessage(id, partitionId)) { }
        public FoxIDsDataException(Exception inner) : base(inner.Message, inner)
        {
            StatusCode = GetStatusCode(inner);
        }
        public FoxIDsDataException(string partitionId, Exception inner) : base(IdToMessage(partitionId), inner)
        {
            StatusCode = GetStatusCode(inner);
        }
        public FoxIDsDataException(string id, string partitionId, Exception inner) : base(IdToMessage(id, partitionId), inner)
        {
            StatusCode = GetStatusCode(inner);
        }
        public FoxIDsDataException(string id, string partitionId, string message, Exception inner) : base($"{IdToMessage(id, partitionId)} {message}", inner)
        {
            StatusCode = GetStatusCode(inner);
        }

        public DataStatusCode? StatusCode { get; set; }

        public override string Message => $"{base.Message}{GetStatusMessage()}";

        private string GetStatusMessage()
        {
            if(StatusCode.HasValue)
            {
                return $" Status '{StatusCode} ({(int)StatusCode})'.";
            }
            return null;
        }

        private DataStatusCode? GetStatusCode(Exception inner)
        {
            if (inner is CosmosException cosmosException)
            {
                if (cosmosException.StatusCode == HttpStatusCode.NotFound)
                {
                    return DataStatusCode.NotFound;
                }
                else if (cosmosException.StatusCode == HttpStatusCode.Conflict)
                {
                    return DataStatusCode.Conflict;
                }
            }

            return null;
        }

        private static string IdToMessage(string partitionId)
        {
            return $"Document partition id '{partitionId}'.";
        }

        private static string IdToMessage(string id, string partitionId)
        {
            return $"Document id '{id}' partition id '{partitionId}'.";
        }
    }
}
