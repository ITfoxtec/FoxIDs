using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;

namespace FoxIDs.Logic
{
    public static class SamlConvertLogic
    {
        public static Saml2StatusCodes ErrorToSamlStatus(string error)
        {
            if (error.IsNullOrEmpty())
            {
                return Saml2StatusCodes.Success;
            }

            switch (error)
            {
                case IdentityConstants.ResponseErrors.LoginRequired:
                    return Saml2StatusCodes.NoAuthnContext;

                default:
                    return Saml2StatusCodes.Responder;
            }
        }
    }
}
