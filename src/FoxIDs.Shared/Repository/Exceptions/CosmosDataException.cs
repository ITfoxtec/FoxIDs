using Microsoft.Azure.Cosmos;
using System;
using System.Net;

namespace FoxIDs.Repository
{
    [Serializable]
    public class CosmosDataException : Exception
    {
        public CosmosDataException() { }
        public CosmosDataException(string partitionId) : base(IdToMessage(partitionId)) { }
        public CosmosDataException(string id, string partitionId) : base(IdToMessage(id, partitionId)) { }

        public CosmosDataException(Exception inner) : base(inner.Message, inner) { }

        public CosmosDataException(string partitionId, Exception inner) : base(IdToMessage(partitionId), inner) { }
        public CosmosDataException(string id, string partitionId, Exception inner) : base(IdToMessage(id, partitionId), inner) { }
        public CosmosDataException(string id, string partitionId, string message, Exception inner) : base($"{IdToMessage(id, partitionId)} {message}", inner) { }

        public HttpStatusCode? StatusCode => (InnerException as CosmosException)?.StatusCode;

        public override string Message => $"{base.Message}{GetStatus()}";

        private string GetStatus()
        {
            if(StatusCode.HasValue)
            {
                return $" Status '{StatusCode} ({(int)StatusCode})'.";
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
