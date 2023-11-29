using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Http;
using ITfoxtec.Identity.Saml2.Schemas;
using System;

namespace FoxIDs.Infrastructure.Saml2
{
    public class FoxIdsSaml2TokenExchangeBinding : Saml2Binding
    {
        public Saml2Request ReadSamlRequest(HttpRequest request, FoxIdsSaml2TokenExchangeRequest foxIdsSaml2TokenExchangeRequest)
        {
            return Read(request, foxIdsSaml2TokenExchangeRequest, Saml2Constants.Message.Assertion, false, false);
        }

        protected override void BindInternal(Saml2Request saml2RequestResponse, string messageName)
        {
            throw new NotImplementedException();
        }

        protected override bool IsRequestResponseInternal(HttpRequest request, string messageName)
        {
            throw new NotImplementedException();
        }

        protected override Saml2Request Read(HttpRequest request, Saml2Request saml2RequestResponse, string messageName, bool validate, bool detectReplayedTokens)
        {
            if (!"DIRECT".Equals(request.Method, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidSaml2BindingException("Not DIRECT binding (DIRECT token exchange).");

            if (string.IsNullOrWhiteSpace(request.Body))
                throw new Saml2BindingException("DIRECT token exchange body is null or empty");

            if(saml2RequestResponse is FoxIdsSaml2TokenExchangeRequest foxIdsSaml2TokenExchangeRequest)
            {
                foxIdsSaml2TokenExchangeRequest.ReadInternal(request.Body, validate, detectReplayedTokens);
                XmlDocument = saml2RequestResponse.XmlDocument;
                return saml2RequestResponse;
            }
            else
            {
                throw new Saml2BindingException($"Not a '{nameof(FoxIdsSaml2TokenExchangeRequest)}' request");
            }
        }

        protected override Saml2Request UnbindInternal(HttpRequest request, Saml2Request saml2RequestResponse, string messageName)
        {
            UnbindInternal(request, saml2RequestResponse);

            return Read(request, saml2RequestResponse, messageName, true, true);
        }
    }
}
