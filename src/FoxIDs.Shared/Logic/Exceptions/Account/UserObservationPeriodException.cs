using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class UserObservationPeriodException : AccountException
    {
        public UserObservationPeriodException() { }
        public UserObservationPeriodException(string message) : base(message) { }
        public UserObservationPeriodException(string message, Exception innerException) : base(message, innerException) { }
        protected UserObservationPeriodException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}