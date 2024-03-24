using ITfoxtec.Identity.Saml2.Schemas;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SamlRequestException : EndpointException
    {
        public Saml2StatusCodes Status { get; set; }

        public SamlRequestException() { }
        public SamlRequestException(string message) : base(message) { }
        public SamlRequestException(string message, Exception inner) : base(message, inner) { }

        public override string Message => $"{base.Message} SAML 2.0 Status '{Status}'"; 
    }
}
