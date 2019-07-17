using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class OAuthRequestException : EndpointException
    {
        public string Error { get; set; }
        public string ErrorDescription { get; }

        public OAuthRequestException() { }
        public OAuthRequestException(string errorDescription) : base(errorDescription)
        {
            ErrorDescription = errorDescription;
        }
        public OAuthRequestException(string errorDescription, Exception inner) : base(errorDescription, inner)
        {
            ErrorDescription = errorDescription;
        }
        protected OAuthRequestException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
