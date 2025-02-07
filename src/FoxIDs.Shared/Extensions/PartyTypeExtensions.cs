using FoxIDs.Models;
using System;

namespace FoxIDs
{
    public static class PartyTypeExtensions
    {
        public static string GetPartyTypeValue(this PartyTypes partyType)
        {

            switch (partyType) 
            {
                case PartyTypes.Login:
                    return "login";
                case PartyTypes.Oidc:
                    return "oidc";
                case PartyTypes.OAuth2:
                    return "oauth2";
                case PartyTypes.Saml2:
                    return "saml2";
                case PartyTypes.TrackLink:
                    return "env_link";
                case PartyTypes.ExternalLogin:
                    return "ext_login";
                default:
                    throw new NotSupportedException();
            }
        }

        public static bool TryGetPartyType(this string partyTypeValue, out PartyTypes partyTypes)
        {
            switch (partyTypeValue?.ToLower())
            {
                case "login":
                    partyTypes = PartyTypes.Login;
                    return true;
                case "oidc":
                    partyTypes = PartyTypes.Oidc;
                    return true;
                case "oauth2":
                    partyTypes = PartyTypes.OAuth2;
                    return true;
                case "saml2":
                    partyTypes = PartyTypes.Saml2;
                    return true;
                case "env_link":
                    partyTypes = PartyTypes.TrackLink;
                    return true;
                case "ext_login":
                    partyTypes = PartyTypes.ExternalLogin;
                    return true;
            }

            partyTypes = PartyTypes.Login;
            return false;
        }
    }
}
