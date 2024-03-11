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
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
