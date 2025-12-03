using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Infrastructure.Saml2
{
    public static class SamlRequestHelper
    {
        public static bool IsAuthnMetadataRequest(HttpRequest request)
        {
            if (!HttpMethods.IsGet(request.Method))
            {
                return false;
            }

            if (request.Query != null && (request.Query.ContainsKey(Saml2Constants.Message.SamlRequest) || request.Query.ContainsKey(Saml2Constants.Message.SamlArt)))
            {
                return false;
            }

            if (request.HasFormContentType && request.Form != null && request.Form.ContainsKey(Saml2Constants.Message.SamlRequest))
            {
                return false;
            }

            return true;
        }
    }
}
