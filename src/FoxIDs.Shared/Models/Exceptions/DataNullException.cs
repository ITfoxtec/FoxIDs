using System;

namespace FoxIDs.Models
{
    [Serializable]
    public class DataNullException : Exception
    {
        public DataNullException() { }
        public DataNullException(string message) : base(message) { }
        public DataNullException(string message, Exception inner) : base(message, inner) { }
        protected DataNullException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
