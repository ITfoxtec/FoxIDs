using System;
using System.Net;
using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    [Serializable]
    public class HttpStatusException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public HttpStatusException() { }
        public HttpStatusException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
        public HttpStatusException(HttpStatusCode statusCode, string message, Exception inner) : base(message, inner)
        {
            StatusCode = statusCode;
        }
        protected HttpStatusException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
