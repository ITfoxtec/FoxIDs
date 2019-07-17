using Microsoft.Azure.Documents;
using System;

namespace FoxIDs.Repository
{
    [Serializable]
    public class CosmosDataException : Exception
    {
        public CosmosDataException() { }
        public CosmosDataException(string partitionId) : base(IdToMessage(partitionId)) { }
        public CosmosDataException(string id, string partitionId) : base(IdToMessage(id, partitionId)) { }

        public CosmosDataException(string partitionId, Exception inner) : base(IdToMessage(partitionId), inner) { }
        public CosmosDataException(string id, string partitionId, Exception inner) : base(IdToMessage(id, partitionId), inner) { }
        public CosmosDataException(string id, string partitionId, string message, Exception inner) : base($"{IdToMessage(id, partitionId)} {message}", inner) { }
        protected CosmosDataException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public override string Message => $"{base.Message}{GetStatus()}";

        private string GetStatus()
        {
            if(InnerException is DocumentClientException)
            {
                var iEx = InnerException as DocumentClientException;
                return $" Status '{iEx.StatusCode} ({(int)iEx.StatusCode})'.";
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
