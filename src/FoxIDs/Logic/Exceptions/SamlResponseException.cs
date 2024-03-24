using ITfoxtec.Identity.Saml2.Schemas;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class SamlResponseException : EndpointException
    {
        public Saml2StatusCodes Status { get; set; }

        public SamlResponseException() { }
        public SamlResponseException(string message) : base(message) { }
        public SamlResponseException(string message, Exception inner) : base(message, inner) { }

        public override string Message => $"{base.Message} SAML 2.0 Status '{Status}'";

    }
}
