using ITfoxtec.Identity.Saml2.Schemas;
using System;
using System.Runtime.Serialization;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SamlRequestException : EndpointException
    {
        public Saml2StatusCodes Status { get; set; }

        public SamlRequestException() { }
        public SamlRequestException(string message) : base(message) { }
        public SamlRequestException(string message, Exception inner) : base(message, inner) { }
        protected SamlRequestException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
