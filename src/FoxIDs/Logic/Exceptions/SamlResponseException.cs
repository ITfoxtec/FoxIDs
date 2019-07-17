using ITfoxtec.Identity.Saml2.Schemas;
using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SamlResponseException : EndpointException
    {
        public Saml2StatusCodes Status { get; set; }

        public SamlResponseException() { }
        public SamlResponseException(string message) : base(message) { }
        public SamlResponseException(string message, Exception inner) : base(message, inner) { }
        protected SamlResponseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
